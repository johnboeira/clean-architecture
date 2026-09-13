using PickupReservations.Api.Extensions;
using PickupReservations.Api.Endpoints.Reservations;
using PickupReservations.Application.Extensions;
using PickupReservations.Infrastructure.Extensions;

var migrate = args.Contains("--migrate", StringComparer.Ordinal);
var hostArgs = args.Where(argument => argument != "--migrate").ToArray();
var builder = WebApplication.CreateBuilder(hostArgs);
var connectionString = builder.Configuration.GetConnectionString("Reservations")
    ?? throw new InvalidOperationException("A conexão Reservations deve ser configurada.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(connectionString);
builder.Services.AddPresentationLayer();

await using var app = builder.Build();

if (migrate)
{
    await app.Services.ApplyInfrastructureMigrationsAsync(app.Lifetime.ApplicationStopping);
    app.Logger.LogInformation("Migrations aplicadas.");
    return;
}

app.UseExceptionHandler();
app.MapReservationEndpoints();
await app.RunAsync();
