using FluentValidation;

namespace HomeInventory.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    /// <summary>Upper bound on a single question, to keep prompt size (and cost) bounded.</summary>
    public const int MaxMessageLength = 2000;

    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(MaxMessageLength);
    }
}
