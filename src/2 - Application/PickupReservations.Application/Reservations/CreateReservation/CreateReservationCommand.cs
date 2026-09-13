using MediatR;

namespace PickupReservations.Application.Reservations.CreateReservation;

public sealed record CreateReservationCommand(string? CustomerName, string? ItemDescription, int Quantity) : IRequest<Guid>;
