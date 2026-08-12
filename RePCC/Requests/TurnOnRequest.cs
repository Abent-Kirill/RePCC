using MediatR;

namespace RePCC.Requests;

internal sealed record TurnOnRequest(Computer Computer) : IRequest;
