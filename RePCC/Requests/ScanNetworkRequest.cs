using MediatR;

namespace RePCC.Requests;

internal sealed record ScanNetworkRequest : IRequest<IEnumerable<Computer>>;
