using Platform.Common.Contracts;
using FluentValidation;

namespace Platform.Common.Validation;

/// <summary>
/// Shape validation for the autocomplete query. The floor on the term is the rule worth
/// naming: a single letter matches most of the table, which is a scan returning a
/// dropdown nobody wants.
/// </summary>
public sealed class UserSearchRequestValidator : AbstractValidator<UserSearchRequest>
{
    public UserSearchRequestValidator()
    {
        RuleFor(request => request.Term)
            .NotEmpty().WithMessage("Enter something to search for.")
            .MinimumLength(UserSearchRequest.MinimumTermLength)
            .WithMessage($"Enter at least {UserSearchRequest.MinimumTermLength} characters.")
            .MaximumLength(64);

        RuleFor(request => request.Limit)
            .InclusiveBetween(1, UserSearchRequest.MaximumLimit)
            .WithMessage($"Ask for between 1 and {UserSearchRequest.MaximumLimit} matches.")
            .When(request => request.Limit.HasValue);
    }
}
