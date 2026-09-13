using PickupReservations.Domain.ReservationAggregate;

namespace PickupReservations.Application.Reservations.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
