using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Households.Queries.GetMyHousehold;

/// <summary>Returns the household the current user belongs to.</summary>
public sealed record GetMyHouseholdQuery : IRequest<Result<HouseholdResponse>>;
