using AutoMapper;
using Platform.Common.Contracts;
using Platform.Common.Identity;

namespace Platform.Common.Mapping;

/// <summary>
/// <see cref="AppUser"/> to the two shapes a client sees them in. Roles live in a
/// separate Identity table rather than on the user row, so they are supplied by the
/// caller through the mapping context instead of being resolved here.
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
