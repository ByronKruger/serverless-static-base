using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;

namespace Coffeeg.Api.MapperProfiles;

/// <summary>
/// <see cref="AppUser"/> to <see cref="UserResponse"/>. Roles live in a separate
/// Identity table rather than on the user row, so they are supplied by the caller
/// through the mapping context instead of being resolved here.
/// </summary>
public sealed class UserProfile : Profile
{
    /// <summary>Context key the caller puts the user's roles under.</summary>
    public const string RolesKey = "Roles";

    public UserProfile() =>
        CreateMap<AppUser, UserResponse>()
            .ForCtorParam(
                nameof(UserResponse.Roles),
                options => options.MapFrom((_, context) =>
                    context.Items.TryGetValue(RolesKey, out var roles)
                        ? (IReadOnlyCollection<string>)roles
                        : []));
}
