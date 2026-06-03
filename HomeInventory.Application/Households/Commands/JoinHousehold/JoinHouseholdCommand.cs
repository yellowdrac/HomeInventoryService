using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Households.Commands.JoinHousehold;

/// <summary>
/// Joins the current user to an existing household identified by its join code and re-issues
/// tokens carrying the new <c>householdId</c> claim.
/// </summary>
public sealed record JoinHouseholdCommand(string JoinCode)
    : IRequest<Result<AuthenticationResponse>>;
