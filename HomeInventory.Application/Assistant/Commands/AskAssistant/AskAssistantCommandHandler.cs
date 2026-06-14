using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HomeInventory.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandHandler
    : IRequestHandler<AskAssistantCommand, Result<ChatResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryAssistant _assistant;
    private readonly IAssistantRateLimiter _rateLimiter;
    private readonly ILogger<AskAssistantCommandHandler> _logger;

    public AskAssistantCommandHandler(
        ICurrentUser currentUser,
        IInventoryAssistant assistant,
        IAssistantRateLimiter rateLimiter,
        ILogger<AskAssistantCommandHandler> logger)
    {
        _currentUser = currentUser;
        _assistant = assistant;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<Result<ChatResponse>> Handle(
        AskAssistantCommand request,
        CancellationToken cancellationToken)
    {
        // The assistant only answers about a household's inventory; require one.
        if (_currentUser.HouseholdId is null)
        {
            return Result.Failure<ChatResponse>(HouseholdErrors.NoHousehold);
        }

        // Bound spend: throttle each user before any (paid) LLM call is made.
        if (!_rateLimiter.TryAcquire(_currentUser.UserId))
        {
            return Result.Failure<ChatResponse>(AssistantErrors.RateLimited);
        }

        try
        {
            var response = await _assistant.AskAsync(
                request.Message,
                request.History ?? [],
                cancellationToken);

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Provider/network failures must not leak as a 500: surface a friendly, expected error.
            _logger.LogError(ex, "The inventory assistant failed to answer a question.");
            return Result.Failure<ChatResponse>(AssistantErrors.Unavailable);
        }
    }
}
