using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Infrastructure.Common.Persistence;
using PickupReservations.Infrastructure.Reservations.Persistence;

namespace PickupReservations.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Reservations");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A conexão Reservations deve ser configurada.");
        }

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
