using HopeTech.Common.Http;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HopeTech.Common.Validation;

/// <summary>
/// Runs the <see cref="IValidator{T}"/> registered for each bound action argument
/// and short-circuits with 400 + <c>ValidationProblemDetails</c>. Validation happens
/// at the edge; services never re-validate shape (Docs/ARCHITECTURE.md).
/// </summary>
public sealed class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var failure in result.Errors)
            {
                context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid && context.Controller is ControllerBase controller)
        {
            // Goes through ProblemDetailsFactory, so the body keeps the same
            // shape (and traceId) as every other error response.
            context.Result = ((ObjectResult)controller.ValidationProblem(context.ModelState)).AsProblemJson();
            return;
        }

        await next();
    }
}
