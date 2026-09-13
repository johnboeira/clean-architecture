using Microsoft.Extensions.DependencyInjection;

namespace PickupReservations.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjectionExtensions).Assembly));

        return services;
    }
}
