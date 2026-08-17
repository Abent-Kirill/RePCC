using MediatR;
using RePCC.Models;

namespace RePCC.Requests;

internal sealed record GetFromDatabaseRequest : IRequest<IReadOnlyCollection<ComputerRecord>>;
