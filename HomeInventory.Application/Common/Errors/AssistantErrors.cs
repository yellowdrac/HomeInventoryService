using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Errors;

/// <summary>Well-known errors for the inventory assistant.</summary>
public static class AssistantErrors
{
    public static readonly Error RateLimited = Error.RateLimited(
        "Assistant.RateLimited",
        "You have sent too many messages. Please wait a moment and try again.");

    public static readonly Error Unavailable = Error.Failure(
        "Assistant.Unavailable",
        "The assistant is temporarily unavailable. Please try again later.");
}
