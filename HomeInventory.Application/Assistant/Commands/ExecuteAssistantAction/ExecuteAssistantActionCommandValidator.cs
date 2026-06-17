using FluentValidation;

namespace HomeInventory.Application.Assistant.Commands.ExecuteAssistantAction;

public sealed class ExecuteAssistantActionCommandValidator
    : AbstractValidator<ExecuteAssistantActionCommand>
{
    public ExecuteAssistantActionCommandValidator()
    {
        RuleFor(x => x.Actions)
            .NotEmpty()
            .WithMessage("At least one action must be provided.");
    }
}
