using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Authentication.Commands.Register;

/// <summary>Registers a new user account. The user is not assigned to a household yet.</summary>
public sealed record RegisterCommand(string Email, string Password, string DisplayName)
    : IRequest<Result<AuthenticationResponse>>;
