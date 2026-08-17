using MediatR;
using RePCC.Models;

namespace RePCC.Requests;

internal sealed record TurnOnRequest(Computer Computer) : IRequest;
