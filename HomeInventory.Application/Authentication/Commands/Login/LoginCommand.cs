using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.Login;

/// <summary>Validates credentials and returns a fresh access + refresh token pair.</summary>
public sealed record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthenticationResponse>>;
