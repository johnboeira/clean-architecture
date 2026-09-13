using MediatR;
using PickupReservations.Application.Reservations.Models;

namespace PickupReservations.Application.Reservations.GetReservationById;

public sealed record GetReservationByIdQuery(Guid Id) : IRequest<ReservationDetails?>;
