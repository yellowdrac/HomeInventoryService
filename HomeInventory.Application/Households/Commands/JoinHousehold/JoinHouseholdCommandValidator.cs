using FluentValidation;

namespace HomeInventory.Application.Households.Commands.JoinHousehold;

public sealed class JoinHouseholdCommandValidator : AbstractValidator<JoinHouseholdCommand>
{
    public JoinHouseholdCommandValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty()
            .Length(8);
    }
}
