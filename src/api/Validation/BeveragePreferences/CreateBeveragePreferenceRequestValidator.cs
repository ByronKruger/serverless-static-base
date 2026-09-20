using Coffeeg.Api.Dtos;
using FluentValidation;

namespace Coffeeg.Api.Validation.BeveragePreferences;

/// <summary>
/// Shape validation only. Whether an ingredient belongs to the beverage type, whether it
/// needs a variant chosen, and whether an amount fits its unit are all domain rules: they
/// need the catalogue to answer, so they live in
/// <see cref="Services.BeveragePreferenceService"/> and come back as <c>Result</c>
/// failures. Duplicating them here would give two definitions that drift.
/// </summary>
public sealed class CreateBeveragePreferenceRequestValidator : AbstractValidator<CreateBeveragePreferenceRequest>
{
    public CreateBeveragePreferenceRequestValidator()
    {
        RuleFor(request => request.BeverageTypeId)
            .GreaterThan(0).WithMessage("Choose a beverage type.");

        RuleFor(request => request.Ingredients)
            .NotNull().WithMessage("Send an ingredients list, even an empty one.");

        RuleForEach(request => request.Ingredients)
            .ChildRules(line =>
            {
                line.RuleFor(ingredient => ingredient.IngredientId)
                    .GreaterThan(0).WithMessage("Choose an ingredient.");

                // A negative amount is nonsense in every unit that exists, so it is shape
                // rather than domain. The unit's own range is checked in the service.
                line.RuleFor(ingredient => ingredient.Amount)
                    .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.")
                    .When(ingredient => ingredient.Amount.HasValue);
            });
    }
}
