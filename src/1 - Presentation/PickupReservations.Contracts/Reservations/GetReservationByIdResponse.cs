namespace PickupReservations.Contracts.Reservations;

public sealed record GetReservationByIdResponse(Guid Id, string CustomerName, string ItemDescription, int Quantity, string Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
