using PickupReservations.Application.Reservations.Models;

namespace PickupReservations.Application.Reservations.Interfaces;

public interface IReservationQueries
{
    Task<ReservationDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
