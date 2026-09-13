using PickupReservations.Domain.Common;

namespace PickupReservations.Domain.ReservationAggregate;

public sealed class Reservation : AggregateRoot
{
    public string CustomerName { get; private set; } = string.Empty;
    public string ItemDescription { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private Reservation() { }

    private Reservation(string customerName, string itemDescription, int quantity, DateTimeOffset now)
        : base(Guid.NewGuid())
    {
        CustomerName = customerName;
        ItemDescription = itemDescription;
        Quantity = quantity;
        Status = ReservationStatus.Open;
        CreatedAt = now.ToUniversalTime();
        ExpiresAt = CreatedAt.AddMinutes(15);
    }

    public static Reservation Create(string? customerName, string? itemDescription, int quantity, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainException("O nome do cliente é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(itemDescription))
        {
            throw new DomainException("A descrição do item é obrigatória.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("A quantidade deve ser maior que zero.");
        }

        return new Reservation(customerName, itemDescription, quantity, now);
    }
}
