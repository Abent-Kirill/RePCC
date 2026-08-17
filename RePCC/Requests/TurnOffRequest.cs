using MediatR;
using RePCC.Models;

namespace RePCC.Requests;

internal sealed record TurnOffRequest(Computer Computer) : IRequest;
