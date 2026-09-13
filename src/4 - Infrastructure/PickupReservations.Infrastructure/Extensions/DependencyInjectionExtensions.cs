using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Infrastructure.Common.Persistence;
using PickupReservations.Infrastructure.Reservations.Persistence;

namespace PickupReservations.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<ReservationsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IReservationQueries, ReservationQueries>();

        return services;
    }

    public static async Task ApplyInfrastructureMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReservationsDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
