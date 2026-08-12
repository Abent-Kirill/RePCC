using MediatR;

namespace RePCC.Requests;

internal sealed record TurnOffRequest(Computer Computer) : IRequest;
