using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Units;

public sealed record GetUnitsQuery : IRequest<Result<IReadOnlyList<UnitDto>>>;
