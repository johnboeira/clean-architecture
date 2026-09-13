using PickupReservations.Domain.ReservationAggregate;

namespace PickupReservations.Application.Reservations.Models;

public sealed record ReservationDetails(Guid Id, string CustomerName, string ItemDescription, int Quantity, ReservationStatus Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
