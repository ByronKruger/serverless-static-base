using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coffeeg.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Tests.Controllers;

/// <summary>
/// Creating and reading preferences over HTTP, through the real pipeline: binding, the
/// validation filter, ownership, Problem Details and the 201 Location header.
///
/// Every test signs in as its own person, because the factory's in-memory database is
/// shared across the class and one preference per beverage type is the rule under test.
/// </summary>
public class BeveragePreferencesControllerTests(CoffeegApiFactory factory) : IClassFixture<CoffeegApiFactory>
{
    private const int Coffee = 1;
    private const int Whiskey = 2;

    private static class Ingredient
    {
        public const int Coffee = 1;
        public const int Milk = 2;
        public const int SoyMilk = 5;
        public const int Honey = 8;
        public const int Water = 9;
        public const int Whiskey = 10;
        public const int Ice = 11;
    }

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

    private static CreateBeveragePreferenceRequest Request(
        int beverageTypeId,
        params (int IngredientId, decimal? Amount)[] lines) =>
        new(beverageTypeId, [.. lines.Select(line =>
            new BeveragePreferenceIngredientRequest(line.IngredientId, line.Amount))]);

    [Fact]
    public async Task Creating_a_preference_requires_authentication()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/beverage-preferences", Request(Coffee, (Ingredient.Coffee, 2m)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_201_with_a_location_that_resolves()
    {
        var client = await SignedInAsync("pref-create");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(
                Coffee,
                (Ingredient.Coffee, 2m),
                (Ingredient.SoyMilk, 40m),
                (Ingredient.Honey, 1m)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);

        Assert.NotNull(created);
        Assert.Equal("Coffee", created.BeverageType.Name);
        Assert.Equal(3, created.Ingredients.Count);

        // The Location header must actually point at the caller's own resource.
        var followed = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);

        var fetched = await followed.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);

        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task Create_returns_each_amount_with_the_unit_its_ingredient_declares()
    {
        var client = await SignedInAsync("pref-units");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Whiskey, 2m), (Ingredient.Ice, 1m)));

        var created = await response.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);

        var ice = created!.Ingredients.Single(line => line.IngredientName == "Ice");
        var whiskey = created.Ingredients.Single(line => line.IngredientName == "Whiskey");

        Assert.Equal("Yes / no", ice.Unit.Name);
        Assert.Equal("Shots", whiskey.Unit.Name);
        Assert.Equal(2m, whiskey.Amount);
    }

    [Fact]
    public async Task Create_rejects_a_second_preference_for_the_same_beverage_type()
    {
        var client = await SignedInAsync("pref-duplicate");

        await client.PostAsJsonAsync("/api/beverage-preferences", Request(Coffee, (Ingredient.Coffee, 1m)));

        var duplicate = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Coffee, 3m)));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);

        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal("preference.duplicate_beverage_type", problem!.Extensions["code"]?.ToString());
        Assert.NotNull(problem.Extensions["traceId"]);
    }

    [Fact]
    public async Task Create_allows_one_preference_per_beverage_type()
    {
        var client = await SignedInAsync("pref-two-types");

        var coffee = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Coffee, 1m)));

        var whiskey = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Whiskey, 2m)));

        Assert.Equal(HttpStatusCode.Created, coffee.StatusCode);
        Assert.Equal(HttpStatusCode.Created, whiskey.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_beverage_type()
    {
        var client = await SignedInAsync("pref-no-type");

        var response = await client.PostAsJsonAsync("/api/beverage-preferences", Request(404));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal("beverage_type.not_found", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Create_rejects_a_complex_ingredient_and_says_which_variants_exist()
    {
        var client = await SignedInAsync("pref-category");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Milk, 50m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal("preference.ingredient_requires_variant", problem!.Extensions["code"]?.ToString());
        Assert.Contains("ingredients[0].amount", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_rejects_an_amount_outside_the_units_range()
    {
        var client = await SignedInAsync("pref-range");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.SoyMilk, 150m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal("preference.amount_out_of_range", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Create_rejects_a_quantity_for_a_yes_no_unit()
    {
        var client = await SignedInAsync("pref-boolean");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Ice, 4m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal("preference.amount_not_boolean", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Create_rejects_a_negative_amount_at_the_edge()
    {
        var client = await SignedInAsync("pref-negative");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Coffee, -1m)));

        // The FluentValidation filter catches this before the action runs, so the code
        // extension is the generic one rather than a domain code.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task List_returns_only_the_callers_own_preferences()
    {
        var mine = await SignedInAsync("pref-mine");
        var theirs = await SignedInAsync("pref-theirs");

        await mine.PostAsJsonAsync("/api/beverage-preferences", Request(Coffee, (Ingredient.Coffee, 1m)));
        await theirs.PostAsJsonAsync("/api/beverage-preferences", Request(Whiskey, (Ingredient.Whiskey, 1m)));

        var response = await mine.GetFromJsonAsync<List<BeveragePreferenceResponse>>(
            "/api/beverage-preferences",
            ApiJson.Options);

        Assert.NotNull(response);
        Assert.Equal(["Coffee"], response.Select(preference => preference.BeverageType.Name));
    }

    [Fact]
    public async Task Somebody_elses_preference_is_not_found_rather_than_forbidden()
    {
        var owner = await SignedInAsync("pref-owner");
        var stranger = await SignedInAsync("pref-stranger");

        var created = await owner.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Coffee, 1m)));

        var theirs = await created.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);

        var response = await stranger.GetAsync($"/api/beverage-preferences/{theirs!.Id}");

        // 404, not 403: a 403 would confirm the id exists.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal("preference.not_found", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task A_null_amount_round_trips_as_none_of_that_ingredient()
    {
        var client = await SignedInAsync("pref-null");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Whiskey, 2m), (Ingredient.Ice, null)));

        var created = await response.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);

        var ice = created!.Ingredients.Single(line => line.IngredientName == "Ice");

        Assert.Null(ice.Amount);
    }

    [Fact]
    public async Task Coffee_rejects_water_because_it_is_assumed_to_be_in_there()
    {
        var client = await SignedInAsync("pref-coffee-water");

        var response = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Coffee, (Ingredient.Water, 1m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal("preference.ingredient_not_in_beverage_type", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Whiskey_takes_water_as_a_splash_rather_than_a_volume()
    {
        var client = await SignedInAsync("pref-splash");

        var tooMuch = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Water, 200m)));

        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);

        var problem = await tooMuch.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal("preference.amount_not_boolean", problem!.Extensions["code"]?.ToString());

        var splash = await client.PostAsJsonAsync(
            "/api/beverage-preferences",
            Request(Whiskey, (Ingredient.Whiskey, 2m), (Ingredient.Water, 1m)));

        Assert.Equal(HttpStatusCode.Created, splash.StatusCode);

        var created = await splash.Content.ReadFromJsonAsync<BeveragePreferenceResponse>(ApiJson.Options);
        var water = created!.Ingredients.Single(line => line.IngredientName == "Water");

        Assert.Equal(1m, water.Amount);
        Assert.Equal("Yes / no", water.Unit.Name);
    }
}
