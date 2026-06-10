using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Items.Commands.DeleteItem;

/// <summary>
/// Deletes an item. Rejected when the item still owns stock lots (empty its stock first).
/// There is no cascade.
/// </summary>
public sealed record DeleteItemCommand(Guid Id) : IRequest<Result>;
