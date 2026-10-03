using HopeTech.Common.Contracts;
using FluentValidation;

namespace HopeTech.Common.Validation;

/// <summary>
/// Presence only, deliberately.
///
/// Nothing stricter belongs here: a format rule could reject an address Identity happily
/// accepted at registration, and any rule beyond presence would leak today's policy to
/// anyone who can reach the login endpoint. A wrong-looking address is not a validation
/// error, it is simply a failed sign-in.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("An email address is required.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("A password is required.");
    }
}
