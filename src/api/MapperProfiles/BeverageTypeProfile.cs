using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;

namespace Coffeeg.Api.MapperProfiles;

/// <summary>
/// The beverage catalogue to its responses.
///
/// Two shapes of the same entity on purpose: the list sends
/// <see cref="BeverageTypeResponse"/> with no ingredients, so a picker stays small,
/// and the detail sends <see cref="BeverageTypeDetailResponse"/> with the whole
/// composition the create form renders from.
/// </summary>
public sealed class BeverageTypeProfile : Profile
{
    public BeverageTypeProfile()
    {
        CreateMap<UnitType, UnitTypeResponse>();

        // Variants maps by name and recurses through this same map, which is what gives
        // a complex ingredient its choices.
        CreateMap<Ingredient, IngredientResponse>()
            .ForCtorParam(
                nameof(IngredientResponse.Unit),
                options => options.MapFrom(ingredient => ingredient.UnitType));

        CreateMap<BeverageType, BeverageTypeResponse>();

        // Composition is a join, so the DTO is projected through it rather than off the
        // navigation directly, and the moderator's display order is preserved.
        CreateMap<BeverageType, BeverageTypeDetailResponse>()
            .ForCtorParam(
                nameof(BeverageTypeDetailResponse.Ingredients),
                options => options.MapFrom(type => type.Ingredients
                    .OrderBy(composition => composition.SortOrder)
                    .ThenBy(composition => composition.Ingredient.Name)
                    .Select(composition => composition.Ingredient)));
    }
}
