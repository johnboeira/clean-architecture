namespace PickupReservations.Contracts.Reservations;

public sealed record CreateReservationRequest(string? CustomerName, string? ItemDescription, int Quantity);
