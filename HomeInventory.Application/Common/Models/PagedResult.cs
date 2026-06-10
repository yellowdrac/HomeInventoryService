namespace HomeInventory.Application.Common.Models;

/// <summary>A single page of <typeparamref name="T"/> results plus pagination metadata.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
