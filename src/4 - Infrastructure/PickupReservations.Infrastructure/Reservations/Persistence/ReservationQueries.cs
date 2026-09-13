using Microsoft.EntityFrameworkCore;
using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Application.Reservations.Models;
using PickupReservations.Infrastructure.Common.Persistence;

namespace PickupReservations.Infrastructure.Reservations.Persistence;

internal sealed class ReservationQueries(ReservationsDbContext context) : IReservationQueries
{
    public Task<ReservationDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == id)
            .Select(reservation => new ReservationDetails(reservation.Id, reservation.CustomerName, reservation.ItemDescription, reservation.Quantity, reservation.Status, reservation.CreatedAt, reservation.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
