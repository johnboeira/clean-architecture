using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PickupReservations.Domain.ReservationAggregate;

namespace PickupReservations.Infrastructure.Reservations.Persistence;

internal sealed class ReservationEntityConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).ValueGeneratedNever();
        builder.Property(reservation => reservation.CustomerName).IsRequired();
        builder.Property(reservation => reservation.ItemDescription).IsRequired();
        builder.Property(reservation => reservation.Quantity).IsRequired();
        builder.Property(reservation => reservation.Status).HasConversion<string>().IsRequired();
        builder.Property(reservation => reservation.CreatedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();
        builder.Property(reservation => reservation.ExpiresAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();
    }
}
