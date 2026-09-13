using Microsoft.AspNetCore.Routing;
using PickupReservations.Api.Middlewares;

namespace PickupReservations.Api.Extensions;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        return services;
    }
}
