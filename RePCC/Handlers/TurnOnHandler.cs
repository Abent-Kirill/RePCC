using MediatR;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed class TurnOnHandler() : IRequestHandler<TurnOnRequest>
{
    public async Task Handle(TurnOnRequest request, CancellationToken cancellationToken) => throw new Exception("работает!");
}

