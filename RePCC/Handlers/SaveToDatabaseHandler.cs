using MediatR;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record SaveToDatabaseHandler(ComputersRepository ComputersRepository) : IRequestHandler<SaveToDatabaseRequest, int>
{
    public async Task<int> Handle(SaveToDatabaseRequest request, CancellationToken cancellationToken) => await ComputersRepository.AddAsync(request.ComputerRecord);
}
