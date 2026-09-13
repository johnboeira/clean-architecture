using MediatR;
using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Domain.ReservationAggregate;

namespace PickupReservations.Application.Reservations.CreateReservation;

public sealed class CreateReservationCommandHandler(IReservationRepository repository, TimeProvider timeProvider) : IRequestHandler<CreateReservationCommand, Guid>
{
    public async Task<Guid> Handle(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reservation = Reservation.Create(command.CustomerName, command.ItemDescription, command.Quantity, timeProvider.GetUtcNow());

        await repository.AddAsync(reservation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}
