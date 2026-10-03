using FluentValidation;
using HopeTech.Api.Dtos;

namespace HopeTech.Api.Validation.Widgets;

/// <summary>
/// Edge validation for <see cref="CreateWidgetRequest"/>. One validator per request
/// type, and never over an EF entity.
///
/// Registered by discovery, not by hand: <c>AddHopeTechApi</c> scans the assemblies it is
/// given, so this is picked up only because <c>Program.cs</c> passes
/// <c>typeof(Program).Assembly</c>. Rules here produce a 400 with per-field messages,
/// before the action runs.
/// </summary>
public sealed class CreateWidgetRequestValidator : AbstractValidator<CreateWidgetRequest>
{
    public CreateWidgetRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(2000);
    }
}
