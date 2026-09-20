using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.MapperProfiles;
using Coffeeg.Api.Results;
using Coffeeg.Api.Services;
using Coffeeg.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Ingredients = Coffeeg.Application.Tests.Fakes.BeverageCatalogue.Ingredients;
using Types = Coffeeg.Application.Tests.Fakes.BeverageCatalogue.Types;

namespace Coffeeg.Application.Tests.Services;

/// <summary>
/// <see cref="BeveragePreferenceService"/> in isolation: fake ports only, no ASP.NET, no
/// DbContext, no database. Both mapper profiles are registered because a preference
/// response nests a beverage type and a unit.
/// </summary>
public class BeveragePreferenceServiceTests
{
    private const int Caller = 7;
    private const int SomebodyElse = 8;

    private readonly FakeBeveragePreferenceRepository _preferences = new();
    private readonly FakeBeverageTypeRepository _beverageTypes = new();
    private readonly FakeClock _clock = new();

    private BeveragePreferenceService CreateService(string? userId = "7")
    {
        IMapper mapper = new MapperConfiguration(
                configuration =>
                {
                    configuration.AddProfile<BeverageTypeProfile>();
                    configuration.AddProfile<BeveragePreferenceProfile>();
                },
                NullLoggerFactory.Instance)
            .CreateMapper();

        return new BeveragePreferenceService(
            _preferences,
            _beverageTypes,
            new FakeCurrentUser(userId),
            _clock,
            mapper);
    }

    private static CreateBeveragePreferenceRequest Request(
        int beverageTypeId,
        params (int IngredientId, decimal? Amount)[] lines) =>
        new(beverageTypeId, [.. lines.Select(line => new BeveragePreferenceIngredientRequest(line.IngredientId, line.Amount))]);

    /// <summary>A coffee: two teaspoons of it, 50% soy milk, one honey. Coffee has no water slot.</summary>
    private static CreateBeveragePreferenceRequest BlackCoffeeWithSoyAndHoney() =>
        Request(
            Types.Coffee,
            (Ingredients.Coffee, 2m),
            (Ingredients.SoyMilk, 50m),
            (Ingredients.Honey, 1m));

    [Fact]
    public async Task Create_saves_the_preference_and_returns_it()
    {
        var result = await CreateService().CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Coffee", result.Value.BeverageType.Name);
        Assert.Equal(3, result.Value.Ingredients.Count);
        Assert.Single(_preferences.Saved);
        Assert.Equal(Caller, _preferences.Saved[0].UserId);
    }

    [Fact]
    public async Task Create_stamps_the_preference_with_the_current_time()
    {
        var result = await CreateService().CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        Assert.Equal(_clock.UtcNow, result.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task Create_returns_each_amount_with_the_unit_its_ingredient_declares()
    {
        var result = await CreateService().CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        var honey = result.Value.Ingredients.Single(line => line.IngredientName == "Honey");
        var soy = result.Value.Ingredients.Single(line => line.IngredientName == "Soy milk");

        Assert.Equal(1m, honey.Amount);
        Assert.Equal("Teaspoons", honey.Unit.Name);
        Assert.Equal(50m, soy.Amount);
        Assert.Equal("Percentage", soy.Unit.Name);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_beverage_type()
    {
        var result = await CreateService().CreateAsync(Request(404), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("beverage_type.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_a_second_preference_for_the_same_beverage_type()
    {
        _preferences.Seed(Caller, Types.Coffee, (Ingredients.Coffee, 1m));

        var result = await CreateService().CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.duplicate_beverage_type", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Create_lets_two_people_each_have_a_coffee_preference()
    {
        _preferences.Seed(SomebodyElse, Types.Coffee, (Ingredients.Coffee, 1m));

        var result = await CreateService().CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_lets_one_person_have_a_preference_per_beverage_type()
    {
        _preferences.Seed(Caller, Types.Coffee, (Ingredients.Coffee, 1m));

        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Whiskey, 2m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_rejects_an_ingredient_from_a_different_beverage()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.Whiskey, 1m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.ingredient_not_in_beverage_type", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_a_complex_ingredient_and_names_its_variants()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.Milk, 50m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.ingredient_requires_variant", result.Error.Code);
        Assert.Contains("Low-fat milk", result.Error.Description);
        Assert.Contains("Soy milk", result.Error.Description);
    }

    [Fact]
    public async Task Create_accepts_a_variant_of_a_complex_ingredient()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.RawMilk, 30m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Raw milk", result.Value.Ingredients.Single().IngredientName);
    }

    [Fact]
    public async Task Create_rejects_the_same_ingredient_twice()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.Coffee, 1m), (Ingredients.Coffee, 2m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.duplicate_ingredient", result.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_an_amount_above_the_units_maximum()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.SoyMilk, 150m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.amount_out_of_range", result.Error.Code);
        Assert.Contains("between 0 and 100", result.Error.Description);
    }

    [Fact]
    public async Task Create_names_the_offending_line_so_the_form_can_highlight_it()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.Coffee, 1m), (Ingredients.SoyMilk, 150m)),
            CancellationToken.None);

        Assert.NotNull(result.Error.Failures);
        Assert.Contains("ingredients[1].amount", result.Error.Failures.Keys);
    }

    [Fact]
    public async Task Create_rejects_a_quantity_for_a_yes_no_unit()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Ice, 3m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.amount_not_boolean", result.Error.Code);
    }

    [Fact]
    public async Task Create_accepts_one_as_yes_for_a_yes_no_unit()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Whiskey, 2m), (Ingredients.Ice, 1m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1m, result.Value.Ingredients.Single(line => line.IngredientName == "Ice").Amount);
    }

    [Fact]
    public async Task Create_accepts_a_null_amount_as_none_of_that_ingredient()
    {
        // The domain's way of saying no: 0 teaspoons, "No" and 0% are one statement
        // (Docs/domain/beverage-preference.md), and null is legal for every unit.
        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Whiskey, 2m), (Ingredients.Ice, null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Ingredients.Single(line => line.IngredientName == "Ice").Amount);
    }

    [Fact]
    public async Task Create_accepts_an_empty_ingredient_list()
    {
        // Saying nothing about any ingredient is the same as wanting none of them.
        var result = await CreateService().CreateAsync(Request(Types.Coffee), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Ingredients);
    }

    [Fact]
    public async Task Create_is_unauthorized_when_the_token_carries_no_usable_id()
    {
        var result = await CreateService(userId: null)
            .CreateAsync(BlackCoffeeWithSoyAndHoney(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task List_returns_only_the_callers_preferences()
    {
        _preferences.Seed(Caller, Types.Coffee, (Ingredients.Coffee, 1m));
        _preferences.Seed(SomebodyElse, Types.Whiskey, (Ingredients.Whiskey, 1m));

        var result = await CreateService().ListMineAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Coffee"], result.Value.Select(preference => preference.BeverageType.Name));
    }

    [Fact]
    public async Task Get_returns_one_of_the_callers_own_preferences()
    {
        var seeded = _preferences.Seed(Caller, Types.Coffee, (Ingredients.Honey, 2m));

        var result = await CreateService().GetMineAsync(seeded.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Honey", result.Value.Ingredients.Single().IngredientName);
    }

    [Fact]
    public async Task Get_reports_somebody_elses_preference_as_not_found()
    {
        var theirs = _preferences.Seed(SomebodyElse, Types.Coffee, (Ingredients.Coffee, 1m));

        var result = await CreateService().GetMineAsync(theirs.Id, CancellationToken.None);

        // Not forbidden: telling the two apart would let a caller probe for ids.
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("preference.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Coffee_does_not_offer_water_at_all()
    {
        // Water is assumed to be in a coffee, so it is not part of the composition and
        // there is nothing useful for a person to say about it.
        var result = await CreateService().CreateAsync(
            Request(Types.Coffee, (Ingredients.Water, 1m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.ingredient_not_in_beverage_type", result.Error.Code);
    }

    [Fact]
    public async Task Water_in_a_whiskey_is_a_yes_no_not_a_volume()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Water, 200m)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("preference.amount_not_boolean", result.Error.Code);
    }

    [Fact]
    public async Task A_whiskey_can_be_saved_with_a_splash_of_water()
    {
        var result = await CreateService().CreateAsync(
            Request(Types.Whiskey, (Ingredients.Whiskey, 2m), (Ingredients.Water, 1m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var water = result.Value.Ingredients.Single(line => line.IngredientName == "Water");

        Assert.Equal(1m, water.Amount);
        Assert.Equal("Yes / no", water.Unit.Name);
    }
}
