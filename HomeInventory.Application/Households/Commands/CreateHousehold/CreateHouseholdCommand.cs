using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Households.Commands.CreateHousehold;

/// <summary>
/// Creates a household owned by the current user, generates a unique join code, assigns the
/// household to the user and re-issues tokens carrying the new <c>householdId</c> claim.
/// </summary>
public sealed record CreateHouseholdCommand(string Name)
    : IRequest<Result<AuthenticationResponse>>;
