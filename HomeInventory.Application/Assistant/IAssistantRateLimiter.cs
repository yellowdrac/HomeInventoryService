namespace HomeInventory.Application.Assistant;

/// <summary>
/// Simple per-user throttle that bounds how often a user can query the (paid) assistant. The
/// concrete implementation lives in Infrastructure.
/// </summary>
public interface IAssistantRateLimiter
{
    /// <summary>
    /// Registers an attempt for <paramref name="userId"/> and returns <c>true</c> when it is within
    /// the allowed rate, or <c>false</c> when the user has exceeded their quota for the window.
    /// </summary>
    bool TryAcquire(Guid userId);
}
