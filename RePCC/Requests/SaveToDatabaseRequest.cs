using MediatR;
using RePCC.Models;

namespace RePCC.Requests;

internal sealed record SaveToDatabaseRequest(ComputerRecord ComputerRecord) : IRequest<int>;
