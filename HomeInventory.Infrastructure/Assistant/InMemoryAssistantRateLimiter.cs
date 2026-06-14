using System.Collections.Concurrent;
using HomeInventory.Application.Assistant;

namespace HomeInventory.Infrastructure.Assistant;

/// <summary>
/// In-memory sliding-window rate limiter: allows up to <c>RateLimitPerMinute</c> assistant questions
/// per user in any rolling 60-second window. Suitable for a single-instance deployment; swap for a
/// distributed store (e.g. Redis) if the API is scaled out.
/// </summary>
public sealed class InMemoryAssistantRateLimiter : IAssistantRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly int _limit;
    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _hits = new();

    public InMemoryAssistantRateLimiter(AssistantOptions options) => _limit = options.RateLimitPerMinute;

    public bool TryAcquire(Guid userId)
    {
        // A non-positive limit disables throttling.
        if (_limit <= 0)
        {
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        var bucket = _hits.GetOrAdd(userId, static _ => new Queue<DateTimeOffset>());

        lock (bucket)
        {
            while (bucket.Count > 0 && now - bucket.Peek() >= Window)
            {
                bucket.Dequeue();
            }

            if (bucket.Count >= _limit)
            {
                return false;
            }

            bucket.Enqueue(now);
            return true;
        }
    }
}
