using MediatR;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record TurnOffHandler : IRequestHandler<TurnOffRequest>
{
    public async Task Handle(TurnOffRequest request, CancellationToken cancellationToken) => await request.Computer.TurnOffAsync(cancellationToken);
}
