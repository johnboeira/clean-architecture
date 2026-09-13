using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PickupReservations.Domain.Common;

namespace PickupReservations.Api.Middlewares;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problem = CreateProblem(exception);
        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro inesperado ao processar {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        var problemContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        };
        await problemDetailsService.WriteAsync(problemContext);
        return true;
    }

    private static ProblemDetails CreateProblem(Exception exception)
    {
        return exception switch
        {
            DomainException domainException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados da reserva inválidos.",
                Detail = domainException.Message
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Requisição inválida."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocorreu um erro inesperado."
            }
        };
    }
}
