using HomeInventory.Domain.Entities;

namespace HomeInventory.Application.Locations.Common;

internal static class LocationSubtree
{
    /// <summary>
    /// Collects the id of <paramref name="rootId"/> plus every descendant location, walking the
    /// parent/child relationships in <paramref name="nodes"/>. Used to scope queries to a location
    /// and everything beneath it (e.g. a whole kitchen/pantry subtree).
    /// </summary>
    public static HashSet<Guid> CollectIds(Guid rootId, IEnumerable<Location> nodes)
    {
        var childrenByParent = nodes
            .Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ids = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(rootId);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!ids.Add(current))
            {
                continue;
            }

            if (childrenByParent.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    pending.Push(child.Id);
                }
            }
        }

        return ids;
    }
}
