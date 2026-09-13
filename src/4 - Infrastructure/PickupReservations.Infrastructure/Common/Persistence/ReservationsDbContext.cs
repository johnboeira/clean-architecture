using Microsoft.EntityFrameworkCore;
using PickupReservations.Domain.ReservationAggregate;
using PickupReservations.Infrastructure.Reservations.Persistence;

namespace PickupReservations.Infrastructure.Common.Persistence;

internal sealed class ReservationsDbContext(DbContextOptions<ReservationsDbContext> options)
    : DbContext(options)
{
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ReservationEntityConfiguration());
    }
}
