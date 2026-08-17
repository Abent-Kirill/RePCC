using MediatR;
using RePCC.Models;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record GetFromDatabaseHandler(ComputersRepository ComputersRepository) : IRequestHandler<GetFromDatabaseRequest, IReadOnlyCollection<ComputerRecord>>
{
    public async Task<IReadOnlyCollection<ComputerRecord>> Handle(GetFromDatabaseRequest request, CancellationToken cancellationToken) => await ComputersRepository.GetComputerRecordsAsync();
}
