using System.Globalization;
using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces;
using Coffeeg.Api.Interfaces.Repositories;
using Coffeeg.Api.Interfaces.Services;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Services;

/// <inheritdoc cref="IBeveragePreferenceService"/>
public sealed class BeveragePreferenceService(
    IBeveragePreferenceRepository preferences,
    IBeverageTypeRepository beverageTypes,
    ICurrentUser currentUser,
    IClock clock,
    IMapper mapper) : IBeveragePreferenceService
{
    public async Task<Result<BeveragePreferenceResponse>> CreateAsync(
        CreateBeveragePreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = CallerId();
        if (caller.IsFailure)
        {
            return Result.Failure<BeveragePreferenceResponse>(caller.Error);
        }

        // The whole composition graph, because every rule below is about it: which
        // ingredients belong to this drink, which of them are categories, and what unit
        // each one is measured in.
        var beverageType = await beverageTypes.FindWithIngredientsAsync(request.BeverageTypeId, cancellationToken);
        if (beverageType is null)
        {
            return BeverageErrors.BeverageTypeNotFound(request.BeverageTypeId);
        }

        if (await preferences.ExistsForUserAsync(caller.Value, beverageType.Id, cancellationToken))
        {
            return BeverageErrors.DuplicateBeverageType(beverageType.Name);
        }

        var lines = BuildLines(request, beverageType);
        if (lines.IsFailure)
        {
            return Result.Failure<BeveragePreferenceResponse>(lines.Error);
        }

        var saved = await preferences.AddAsync(
            new BeveragePreference
            {
                UserId = caller.Value,
                BeverageTypeId = beverageType.Id,
                CreatedAtUtc = clock.UtcNow,
                Ingredients = lines.Value
            },
            cancellationToken);

        return mapper.Map<BeveragePreferenceResponse>(saved);
    }

    public async Task<Result<IReadOnlyCollection<BeveragePreferenceResponse>>> ListMineAsync(
        CancellationToken cancellationToken)
    {
        var caller = CallerId();
        if (caller.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<BeveragePreferenceResponse>>(caller.Error);
        }

        var mine = await preferences.ListForUserAsync(caller.Value, cancellationToken);

        return Result.Success(mapper.Map<IReadOnlyCollection<BeveragePreferenceResponse>>(mine));
    }

    public async Task<Result<BeveragePreferenceResponse>> GetMineAsync(
        int preferenceId,
        CancellationToken cancellationToken)
    {
        var caller = CallerId();
        if (caller.IsFailure)
        {
            return Result.Failure<BeveragePreferenceResponse>(caller.Error);
        }

        var preference = await preferences.FindForUserAsync(caller.Value, preferenceId, cancellationToken);
        if (preference is null)
        {
            return BeverageErrors.PreferenceNotFound(preferenceId);
        }

        return mapper.Map<BeveragePreferenceResponse>(preference);
    }

    /// <summary>
    /// The caller, as the int key the preference rows are joined on. Ownership is this
    /// feature's security boundary, so an unusable id is an authentication failure rather
    /// than a query that quietly matches nothing.
    /// </summary>
    private Result<int> CallerId() =>
        currentUser.UserKey is { } userKey
            ? userKey
            : Error.Unauthorized(
                "auth.unidentified_caller",
                "The request does not identify a signed-in person.");

    /// <summary>
    /// Turns the requested lines into entities, rejecting the first that breaks a rule.
    /// Fail-fast rather than collecting: each error names the field it came from, so the
    /// caller can fix one and resubmit, and the stable code stays unambiguous.
    /// </summary>
    private static Result<List<BeveragePreferenceIngredient>> BuildLines(
        CreateBeveragePreferenceRequest request,
        BeverageType beverageType)
    {
        var choosable = Choosable(beverageType);
        var lines = new List<BeveragePreferenceIngredient>();
        var seen = new HashSet<int>();
        var index = 0;

        foreach (var requested in request.Ingredients)
        {
            // Matches how the model binder names a collection element, so the web form
            // can highlight the offending row.
            var field = $"ingredients[{index.ToString(CultureInfo.InvariantCulture)}].amount";
            index++;

            if (!choosable.TryGetValue(requested.IngredientId, out var ingredient))
            {
                return Result.Failure<List<BeveragePreferenceIngredient>>(
                    NotChoosable(field, beverageType, requested.IngredientId));
            }

            if (!seen.Add(ingredient.Id))
            {
                return Result.Failure<List<BeveragePreferenceIngredient>>(
                    BeverageErrors.DuplicateIngredient(field, ingredient.Name));
            }

            var amount = ValidateAmount(field, ingredient, requested.Amount);
            if (amount.IsFailure)
            {
                return Result.Failure<List<BeveragePreferenceIngredient>>(amount.Error);
            }

            lines.Add(new BeveragePreferenceIngredient
            {
                IngredientId = ingredient.Id,
                Amount = requested.Amount
            });
        }

        return lines;
    }

    /// <summary>
    /// Why an id could not be chosen. Naming a category rather than one of its variants
    /// is a different mistake from naming something that is not in the drink at all, and
    /// it is the one a person actually makes, so it gets its own code and message.
    /// </summary>
    private static Error NotChoosable(string field, BeverageType beverageType, int ingredientId)
    {
        var category = beverageType.Ingredients
            .Select(composition => composition.Ingredient)
            .FirstOrDefault(ingredient => ingredient.Id == ingredientId && ingredient.Variants.Count > 0);

        return category is null
            ? BeverageErrors.IngredientNotInBeverageType(field, ingredientId, beverageType.Name)
            : BeverageErrors.IngredientRequiresVariant(
                field,
                category.Name,
                category.Variants.Select(variant => variant.Name));
    }

    /// <summary>
    /// An amount is read through the unit the ingredient declares - the caller never
    /// supplies one. Null is always legal: it is how the domain says "none of this", and
    /// 0 teaspoons, <c>No</c> and 0% are the same statement.
    /// </summary>
    private static Result ValidateAmount(string field, Ingredient ingredient, decimal? amount)
    {
        if (amount is null)
        {
            return Result.Success();
        }

        var unit = ingredient.UnitType;

        // A Boolean unit carries presence, not quantity, so there is nothing between
        // "yes" and "no" for it to mean.
        if (unit.Kind is UnitKind.Boolean)
        {
            return amount == 1m
                ? Result.Success()
                : Result.Failure(BeverageErrors.AmountNotBoolean(field, ingredient.Name));
        }

        if (unit.MinValue is { } minimum && amount < minimum)
        {
            return Result.Failure(BeverageErrors.AmountOutOfRange(field, ingredient.Name, Describe(unit)));
        }

        if (unit.MaxValue is { } maximum && amount > maximum)
        {
            return Result.Failure(BeverageErrors.AmountOutOfRange(field, ingredient.Name, Describe(unit)));
        }

        return Result.Success();
    }

    /// <summary>
    /// What a caller may actually pick, keyed by id: a simple ingredient stands for
    /// itself, and a complex one is replaced by its variants. Composition names the
    /// category ("Milk"); the choice is the variant ("Soy milk").
    /// </summary>
    private static Dictionary<int, Ingredient> Choosable(BeverageType beverageType)
    {
        var choosable = new Dictionary<int, Ingredient>();

        foreach (var ingredient in beverageType.Ingredients.Select(composition => composition.Ingredient))
        {
            if (ingredient.Variants.Count == 0)
            {
                choosable[ingredient.Id] = ingredient;
                continue;
            }

            foreach (var variant in ingredient.Variants)
            {
                choosable[variant.Id] = variant;
            }
        }

        return choosable;
    }

    /// <summary>The unit's accepted range, phrased for a person.</summary>
    private static string Describe(UnitType unit) => (unit.MinValue, unit.MaxValue) switch
    {
        ({ } minimum, { } maximum) => $"between {Format(minimum)} and {Format(maximum)}",
        ({ } minimum, null) => $"at least {Format(minimum)}",
        (null, { } maximum) => $"at most {Format(maximum)}",
        _ => "a number"
    };

    private static string Format(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
