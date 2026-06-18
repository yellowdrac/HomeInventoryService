using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Common.Text;
using HomeInventory.Application.Items.Common;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Queries.SearchInventory;

public sealed class SearchInventoryQueryHandler
    : IRequestHandler<SearchInventoryQuery, Result<PagedResult<SearchResultItemDto>>>
{
    /// <summary>A query of only digits this long (or longer) is treated as a barcode.</summary>
    private const int BarcodeMinLength = 6;

    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public SearchInventoryQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<PagedResult<SearchResultItemDto>>> Handle(
        SearchInventoryQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<PagedResult<SearchResultItemDto>>(HouseholdErrors.NoHousehold);
        }

        // Normalize the term with the same helper that fills Item.NormalizedName, so the search
        // matches regardless of case and accents.
        var term = TextNormalization.Normalize(request.Query);
        var rawQuery = request.Query.Trim();
        var isCodeLike = rawQuery.Length >= BarcodeMinLength && rawQuery.All(char.IsDigit);

        var itemsQuery = _context.Items.Where(i => i.HouseholdId == householdId);
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            itemsQuery = itemsQuery.Where(i => i.Category == request.Category);
        }

        // Small per-household dataset: load once and rank in memory (mirrors the approach used by
        // the location queries) so the search stays provider-agnostic.
        var items = await itemsQuery.Include(i => i.Unit).ToListAsync(cancellationToken);

        var ranked = items
            .Select(item => Rank(item, term, rawQuery, isCodeLike))
            .Where(match => match is not null)
            .Select(match => match!.Value)
            .OrderBy(match => match.Rank)
            .ThenByDescending(match => match.Similarity)
            .ThenBy(match => match.Item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = ranked.Count;

        var pageMatches = ranked
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var itemIds = pageMatches.Select(m => m.Item.Id).ToList();

        // Stock lots of just the items on this page, scoped to the household.
        var lots = await _context.StockLots
            .Where(s => s.HouseholdId == householdId && itemIds.Contains(s.ItemId))
            .ToListAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var lotsByItem = lots
            .GroupBy(l => l.ItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = pageMatches
            .Select(match => BuildResult(match.Item, lotsByItem, locationsById))
            .ToList();

        return new PagedResult<SearchResultItemDto>(results, request.Page, request.PageSize, totalCount);
    }

    private static RankedItem? Rank(Item item, string term, string rawQuery, bool isCodeLike)
    {
        // An exact barcode match is the strongest signal and wins regardless of the name.
        if (isCodeLike && string.Equals(item.Barcode, rawQuery, StringComparison.Ordinal))
        {
            return new RankedItem(item, MatchRank.BarcodeExact, 1d);
        }

        if (term.Length == 0)
        {
            return null;
        }

        var name = item.NormalizedName;

        if (name == term)
        {
            return new RankedItem(item, MatchRank.Exact, 1d);
        }

        if (name.StartsWith(term, StringComparison.Ordinal))
        {
            return new RankedItem(item, MatchRank.StartsWith, TrigramSimilarity.Compute(name, term));
        }

        if (name.Contains(term, StringComparison.Ordinal))
        {
            return new RankedItem(item, MatchRank.Contains, TrigramSimilarity.Compute(name, term));
        }

        // Fall back to fuzzy trigram similarity to tolerate typos (e.g. "duracel" -> "duracell").
        var similarity = TrigramSimilarity.Compute(name, term);
        return similarity >= TrigramSimilarity.DefaultThreshold
            ? new RankedItem(item, MatchRank.Similar, similarity)
            : null;
    }

    private static SearchResultItemDto BuildResult(
        Item item,
        IReadOnlyDictionary<Guid, List<StockLot>> lotsByItem,
        IReadOnlyDictionary<Guid, Location> locationsById)
    {
        var itemLots = lotsByItem.TryGetValue(item.Id, out var lots) ? lots : [];

        var placements = itemLots
            .Select(lot => new SearchPlacementDto(
                lot.LocationId,
                locationsById.TryGetValue(lot.LocationId, out var location) ? location.Name : string.Empty,
                LocationMappingExtensions.BuildBreadcrumb(lot.LocationId, locationsById),
                lot.Quantity,
                lot.ExpirationDate))
            .OrderBy(p => p.LocationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.ExpirationDate ?? DateOnly.MaxValue)
            .ToList();

        var totalQuantity = itemLots.Sum(l => l.Quantity);

        return new SearchResultItemDto(
            item.Id,
            item.Name,
            item.Category,
            item.TrackingType,
            item.Unit?.Symbol,
            totalQuantity,
            placements);
    }

    /// <summary>Relevance tiers, ordered so the lowest value is the most relevant.</summary>
    private enum MatchRank
    {
        BarcodeExact = 0,
        Exact = 1,
        StartsWith = 2,
        Contains = 3,
        Similar = 4,
    }

    private readonly record struct RankedItem(Item Item, MatchRank Rank, double Similarity);
}
