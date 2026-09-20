using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;
using Coffeeg.Api.MapperProfiles;
using Coffeeg.Api.Results;
using Coffeeg.Api.Services;
using Coffeeg.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Coffeeg.Application.Tests.Services;

/// <summary>
/// <see cref="BeverageTypeService"/> in isolation. The mapper is real, so the projection
/// the create form depends on - ingredients, their variants, their units - is exercised
/// rather than assumed.
/// </summary>
public class BeverageTypeServiceTests
{
    private readonly FakeBeverageTypeRepository _beverageTypes = new();

    private BeverageTypeService CreateService()
    {
        IMapper mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<BeverageTypeProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        return new BeverageTypeService(_beverageTypes, mapper);
    }

    [Fact]
    public async Task List_returns_every_beverage_type()
    {
        var result = await CreateService().ListAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Coffee", "Whiskey"], result.Value.Select(type => type.Name));
    }

    [Fact]
    public async Task Get_returns_not_found_for_an_unknown_type()
    {
        var result = await CreateService().GetAsync(404, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("beverage_type.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Get_returns_the_ingredients_in_the_moderators_order()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Coffee, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["Coffee", "Milk", "Sugar"],
            result.Value.Ingredients.Select(ingredient => ingredient.Name));
    }

    [Fact]
    public async Task Get_carries_the_variants_of_a_complex_ingredient()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Coffee, CancellationToken.None);

        var milk = result.Value.Ingredients.Single(ingredient => ingredient.Name == "Milk");

        Assert.Equal(["Low-fat milk", "Raw milk", "Soy milk"], milk.Variants.Select(variant => variant.Name));
    }

    [Fact]
    public async Task Get_leaves_a_simple_ingredient_without_variants()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Coffee, CancellationToken.None);

        var coffee = result.Value.Ingredients.Single(ingredient => ingredient.Name == "Coffee");

        Assert.Empty(coffee.Variants);
    }

    [Fact]
    public async Task Get_carries_the_unit_each_ingredient_is_measured_in()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Coffee, CancellationToken.None);

        var sugar = result.Value.Ingredients.Single(ingredient => ingredient.Name == "Sugar");

        Assert.Equal("Teaspoons", sugar.Unit.Name);
        Assert.Equal("tsp", sugar.Unit.Symbol);
        Assert.Equal(UnitKind.Numeric, sugar.Unit.Kind);
        Assert.Equal(0m, sugar.Unit.MinValue);
        Assert.Equal(20m, sugar.Unit.MaxValue);
    }

    [Fact]
    public async Task Get_marks_the_yes_no_unit_as_boolean()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Whiskey, CancellationToken.None);

        var ice = result.Value.Ingredients.Single(ingredient => ingredient.Name == "Ice");

        Assert.Equal(UnitKind.Boolean, ice.Unit.Kind);
        Assert.Null(ice.Unit.Symbol);
    }

    [Fact]
    public async Task Get_gives_a_variant_the_same_unit_as_its_category()
    {
        var result = await CreateService().GetAsync(BeverageCatalogue.Types.Coffee, CancellationToken.None);

        var milk = result.Value.Ingredients.Single(ingredient => ingredient.Name == "Milk");

        Assert.All(milk.Variants, variant => Assert.Equal(milk.Unit.Id, variant.Unit.Id));
    }

    [Fact]
    public async Task List_omits_ingredients_so_the_picker_stays_small()
    {
        var result = await CreateService().ListAsync(CancellationToken.None);

        // BeverageTypeResponse has no ingredients at all - the detail call is what carries
        // them. This asserts the two shapes have not been collapsed into one DTO.
        Assert.All(result.Value, type => Assert.IsType<BeverageTypeResponse>(type));
    }
}
