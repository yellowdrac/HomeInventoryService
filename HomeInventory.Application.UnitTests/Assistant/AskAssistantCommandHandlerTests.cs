using FluentAssertions;
using HomeInventory.Application.Assistant;
using HomeInventory.Application.Assistant.Commands.AskAssistant;
using HomeInventory.Application.Assistant.Common;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class AskAssistantCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IInventoryAssistant _assistant = Substitute.For<IInventoryAssistant>();
    private readonly IAssistantRateLimiter _rateLimiter = Substitute.For<IAssistantRateLimiter>();

    private AskAssistantCommandHandler BuildHandler()
    {
        _currentUser.UserId.Returns(_userId);
        _currentUser.HouseholdId.Returns(_householdId);
        _rateLimiter.TryAcquire(Arg.Any<Guid>()).Returns(true);
        return new AskAssistantCommandHandler(
            _currentUser, _assistant, _rateLimiter, NullLogger<AskAssistantCommandHandler>.Instance);
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = new AskAssistantCommandHandler(
            _currentUser, _assistant, _rateLimiter, NullLogger<AskAssistantCommandHandler>.Instance);

        var result = await handler.Handle(new AskAssistantCommand("hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
        await _assistant.DidNotReceiveWithAnyArgs()
            .AskAsync(default!, default!, default);
    }

    [Fact]
    public async Task Fails_when_the_user_is_rate_limited()
    {
        var handler = BuildHandler();
        _rateLimiter.TryAcquire(_userId).Returns(false);

        var result = await handler.Handle(new AskAssistantCommand("hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AssistantErrors.RateLimited);
        await _assistant.DidNotReceiveWithAnyArgs().AskAsync(default!, default!, default);
    }

    [Fact]
    public async Task Delegates_to_the_assistant_and_returns_its_response()
    {
        var handler = BuildHandler();
        var history = new List<ChatMessage> { new("user", "prev") };
        var expected = new ChatResponse("the answer", []);
        _assistant.AskAsync("question", history, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await handler.Handle(
            new AskAssistantCommand("question", history), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
        _rateLimiter.Received(1).TryAcquire(_userId);
    }

    [Fact]
    public async Task Returns_an_unavailable_error_when_the_assistant_throws()
    {
        var handler = BuildHandler();
        _assistant.AskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns<Task<ChatResponse>>(_ => throw new HttpRequestException("provider down"));

        var result = await handler.Handle(new AskAssistantCommand("hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AssistantErrors.Unavailable);
    }
}
