using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;

namespace Coffeeg.Api.MapperProfiles;

/// <summary>
/// <see cref="AppUser"/> to the three shapes a client sees them in. Roles live in a
/// separate Identity table rather than on the user row, so they are supplied by the
/// caller through the mapping context instead of being resolved here.
///
/// There is no map to <see cref="UserProfileResponse"/>. That response needs the person's
/// preferences, which are entities from a different port needing their own map, so
/// <c>UserService</c> composes it from the summary and the mapped preferences. Doing it
/// through a context key as <see cref="RolesKey"/> does would mean nesting
/// <c>context.Mapper.Map</c> inside a member mapping, which is harder to read than the
/// line of composition it saves. That also makes this profile a dependency of
/// <see cref="BeveragePreferenceProfile"/> and <see cref="BeverageTypeProfile"/> in
/// practice: a unit test covering a profile response needs all three registered.
/// </summary>
public sealed class UserProfile : Profile
{
    /// <summary>Context key the caller puts the user's roles under.</summary>
    public const string RolesKey = "Roles";

    public UserProfile()
    {
        CreateMap<AppUser, UserResponse>()
            .ForCtorParam(
                nameof(UserResponse.Roles),
                options => options.MapFrom((_, context) =>
                    context.Items.TryGetValue(RolesKey, out var roles)
                        ? (IReadOnlyCollection<string>)roles
                        : []));

        // Every member matches by name, and the omissions are the point: no email, and
        // no roles - what somebody else may do is not a searcher's business.
        CreateMap<AppUser, UserSummaryResponse>();
    }
}
