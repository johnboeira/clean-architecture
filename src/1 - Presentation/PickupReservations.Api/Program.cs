using PickupReservations.Api.Extensions;
using PickupReservations.Api.Endpoints.Reservations;
using PickupReservations.Application.Extensions;
using PickupReservations.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddPresentationLayer();

await using var app = builder.Build();

await app.Services.ApplyInfrastructureMigrationsAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.MapReservationEndpoints();
await app.RunAsync();
