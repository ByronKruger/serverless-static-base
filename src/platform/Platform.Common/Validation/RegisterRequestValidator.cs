using Platform.Common.Contracts;
using FluentValidation;

namespace Platform.Common.Validation;

/// <summary>
/// Shape validation for registration. Password strength is Identity's job - it owns the
/// policy, and duplicating the rules here would let the two drift apart.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("An email address is required.")
            .MaximumLength(256)
            .EmailAddress().WithMessage("That does not look like an email address.");

        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("A username is required.")
            .MinimumLength(3).WithMessage("A username must be at least 3 characters.")
            .MaximumLength(64)
            // Identity rejects anything outside its allowed set with a less helpful
            // message, so the same constraint is stated plainly here.
            .Matches("^[A-Za-z0-9._@+-]+$")
            .WithMessage("A username may only contain letters, digits and . _ @ + -");

        // No character class on either name. UserName's allowed set above is Identity's
        // constraint restated; a person's name is not Identity's business, and apostrophes,
        // hyphens, spaces and accents are all ordinary in one.
        RuleFor(request => request.FirstName)
            .NotEmpty().WithMessage("A first name is required.")
            .MaximumLength(64);

        RuleFor(request => request.LastName)
            .NotEmpty().WithMessage("A surname is required.")
            .MaximumLength(64);

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("A password is required.")
            .MaximumLength(128);
    }
}
