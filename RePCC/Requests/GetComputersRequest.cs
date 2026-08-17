using MediatR;
using RePCC.Models;

namespace RePCC.Requests;

internal sealed record GetComputersRequest : IRequest<IReadOnlyCollection<Computer>>;
