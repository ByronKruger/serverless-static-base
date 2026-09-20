using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;

namespace Coffeeg.Api.MapperProfiles;

/// <summary>
/// Saved preferences to their responses. Depends on
/// <see cref="BeverageTypeProfile"/> for the nested beverage type and unit maps, so both
/// profiles are needed together - the container adds every profile in the assembly, and
/// a unit test must add both.
/// </summary>
public sealed class BeveragePreferenceProfile : Profile
{
    public BeveragePreferenceProfile()
    {
        CreateMap<BeveragePreference, BeveragePreferenceResponse>();

        // The unit comes from the ingredient, never from the line: that is the whole
        // reason an amount means the same thing every time it is read.
        CreateMap<BeveragePreferenceIngredient, BeveragePreferenceIngredientResponse>()
            .ForCtorParam(
                nameof(BeveragePreferenceIngredientResponse.IngredientName),
                options => options.MapFrom(line => line.Ingredient.Name))
            .ForCtorParam(
                nameof(BeveragePreferenceIngredientResponse.Unit),
                options => options.MapFrom(line => line.Ingredient.UnitType));
    }
}
