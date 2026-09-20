using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;

namespace Coffeeg.Api.Tests.Controllers;

/// <summary>
/// The beverage catalogue over HTTP, through the real pipeline. These also prove the
/// migration's seed data is there: the rows come from <c>HasData</c>, not from anything
/// the test sets up.
/// </summary>
public class BeverageTypesControllerTests(CoffeegApiFactory factory) : IClassFixture<CoffeegApiFactory>
{
    private readonly CoffeegApiFactory _factory = factory;

    private async Task<HttpClient> SignedInAsync(string discriminator)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"{discriminator}@coffeeg.test", discriminator, "Str0ng!Passw0rd"));

        var session = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        return client;
    }

    [Fact]
    public async Task Listing_the_catalogue_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/beverage-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_the_seeded_beverage_types()
    {
        var client = await SignedInAsync("bt-list");

        var response = await client.GetAsync("/api/beverage-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var types = await response.Content.ReadFromJsonAsync<List<BeverageTypeResponse>>();

        Assert.NotNull(types);
        Assert.Contains(types, type => type.Name == "Coffee");
        Assert.Contains(types, type => type.Name == "Whiskey");
    }

    [Fact]
    public async Task Get_returns_the_composition_the_create_form_renders_from()
    {
        var client = await SignedInAsync("bt-detail");

        var response = await client.GetAsync("/api/beverage-types/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var coffee = await response.Content.ReadFromJsonAsync<BeverageTypeDetailResponse>(ApiJson.Options);

        Assert.NotNull(coffee);
        Assert.Equal("Coffee", coffee.Name);
        // Water is deliberately absent: a coffee is assumed to have it.
        Assert.Equal(["Coffee", "Milk", "Sugar"], coffee.Ingredients.Select(i => i.Name));

        var milk = coffee.Ingredients.Single(ingredient => ingredient.Name == "Milk");
        Assert.Equal(["Low-fat milk", "Raw milk", "Soy milk"], milk.Variants.Select(variant => variant.Name));
    }

    [Fact]
    public async Task Get_serializes_the_unit_kind_as_a_string()
    {
        var client = await SignedInAsync("bt-unit");

        var json = await client.GetStringAsync("/api/beverage-types/2");

        // Enums as strings (Docs/ARCHITECTURE.md) - a client reads "Boolean", not 1.
        Assert.Contains("\"kind\":\"Boolean\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"Numeric\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_returns_the_range_a_unit_accepts()
    {
        var client = await SignedInAsync("bt-range");

        var whiskey = await client.GetFromJsonAsync<BeverageTypeDetailResponse>("/api/beverage-types/2", ApiJson.Options);

        var shots = whiskey!.Ingredients.Single(ingredient => ingredient.Name == "Whiskey").Unit;

        Assert.Equal(UnitKind.Numeric, shots.Kind);
        Assert.Equal(0m, shots.MinValue);
        Assert.Equal(10m, shots.MaxValue);
    }

    [Fact]
    public async Task Get_returns_a_problem_document_for_an_unknown_type()
    {
        var client = await SignedInAsync("bt-missing");

        var response = await client.GetAsync("/api/beverage-types/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();

        Assert.Equal("beverage_type.not_found", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Whiskey_measures_water_as_a_yes_no()
    {
        var client = await SignedInAsync("bt-water");

        var whiskey = await client.GetFromJsonAsync<BeverageTypeDetailResponse>(
            "/api/beverage-types/2",
            ApiJson.Options);

        var water = whiskey!.Ingredients.Single(ingredient => ingredient.Name == "Water");

        // An optional splash, not a volume - so it is presence, not quantity.
        Assert.Equal(UnitKind.Boolean, water.Unit.Kind);
        Assert.Null(water.Unit.Symbol);
    }
}
