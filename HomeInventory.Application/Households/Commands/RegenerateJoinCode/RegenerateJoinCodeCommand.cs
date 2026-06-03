using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Households.Commands.RegenerateJoinCode;

/// <summary>Regenerates the join code of the current user's household. Owner-only.</summary>
public sealed record RegenerateJoinCodeCommand : IRequest<Result<HouseholdResponse>>;
