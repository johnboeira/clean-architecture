using MediatR;
using PickupReservations.Application.Reservations.Interfaces;
using PickupReservations.Application.Reservations.Models;

namespace PickupReservations.Application.Reservations.GetReservationById;

public sealed class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, ReservationDetails?>
{
    private readonly IReservationQueries _queries;

    public GetReservationByIdQueryHandler(IReservationQueries queries)
    {
        _queries = queries;
    }

    public Task<ReservationDetails?> Handle(GetReservationByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _queries.GetByIdAsync(query.Id, cancellationToken);
    }
}
