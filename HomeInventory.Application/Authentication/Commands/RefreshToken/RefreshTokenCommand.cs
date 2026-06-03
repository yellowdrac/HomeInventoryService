using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.RefreshToken;

/// <summary>Exchanges a valid refresh token for a new access + refresh token pair (with rotation).</summary>
public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<AuthenticationResponse>>;
