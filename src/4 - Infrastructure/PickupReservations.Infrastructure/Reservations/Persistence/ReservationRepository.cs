using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Domain.ReservationAggregate;
using PickupReservations.Infrastructure.Common.Persistence;

namespace PickupReservations.Infrastructure.Reservations.Persistence;

internal sealed class ReservationRepository : IReservationRepository
{
    private readonly ReservationsDbContext _context;

    public ReservationRepository(ReservationsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
