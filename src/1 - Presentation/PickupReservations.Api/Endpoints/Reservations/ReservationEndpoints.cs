using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using PickupReservations.Application.Reservations.CreateReservation;
using PickupReservations.Application.Reservations.GetReservationById;
using PickupReservations.Contracts.Reservations;

namespace PickupReservations.Api.Endpoints.Reservations;

internal static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/reservations").WithTags("Reservations");
        group.MapPost("/", CreateAsync).ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet("/{id}", GetByIdAsync).WithName("GetReservationById")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return endpoints;
    }

    private static async Task<CreatedAtRoute<CreateReservationResponse>> CreateAsync(CreateReservationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateReservationCommand(request.CustomerName, request.ItemDescription, request.Quantity);
        var id = await sender.Send(command, cancellationToken);

        return TypedResults.CreatedAtRoute(new CreateReservationResponse(id), "GetReservationById", new { id });
    }

    private static async Task<Results<Ok<GetReservationByIdResponse>, ProblemHttpResult>> GetByIdAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var reservation = await sender.Send(new GetReservationByIdQuery(id), cancellationToken);
        if (reservation is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Reserva não encontrada.");
        }

        var response = new GetReservationByIdResponse(reservation.Id, reservation.CustomerName, reservation.ItemDescription, reservation.Quantity, reservation.Status.ToString(), reservation.CreatedAt, reservation.ExpiresAt);
        return TypedResults.Ok(response);
    }
}
