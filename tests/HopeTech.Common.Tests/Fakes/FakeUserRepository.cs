using HopeTech.Common.Identity;
using HopeTech.Common.Abstractions;
using HopeTech.Common.Results;

namespace HopeTech.Common.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IUserRepository"/>. Lets the service be tested without Identity,
/// EF, or a database - the whole point of putting a port in front of them.
/// </summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<AppUser> _users = [];
    private readonly Dictionary<int, string> _passwords = [];
    private readonly Dictionary<int, List<string>> _roles = [];

    private int _nextId = 1;

    /// <summary>Set to fail the next <see cref="CreateAsync"/>, as Identity would on a weak password.</summary>
    public Error? CreateFailure { get; set; }

    /// <summary>Roles granted through <see cref="AddToRoleAsync"/>, for assertions.</summary>
    public IReadOnlyList<string> GrantedRoles => _roles.Values.SelectMany(roles => roles).ToList();

    /// <summary>
    /// Seeds an account with a password and roles, and no names - the shape of an account
    /// registered before D-0007.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> an overload taking the names before <c>params roles</c>.
    /// Given both, C# binds <c>Seed(email, user, pwd, Roles.User, Roles.Moderator)</c> to
    /// the names rather than to the roles, because a non-params match beats a params
    /// expansion - which silently granted no roles at all and failed one auth test.
    /// </remarks>
    public AppUser Seed(string email, string userName, string password, params string[] roles)
    {
        var user = Add(email, userName, firstName: null, lastName: null);

        _passwords[user.Id] = password;
        _roles[user.Id] = [.. roles];

        return user;
    }

    /// <summary>Adds a user with the given identity fields.</summary>
    private AppUser Add(string email, string userName, string? firstName, string? lastName)
    {
        var user = new AppUser
        {
            Id = _nextId++,
            Email = email,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName
        };

        _users.Add(user);
        _roles[user.Id] = [];

        return user;
    }

    public Task<AppUser?> FindByIdAsync(int userId, CancellationToken cancellationToken) =>
        Task.FromResult(_users.SingleOrDefault(user => user.Id == userId));

    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.SingleOrDefault(user =>
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        Task.FromResult(_users.SingleOrDefault(user =>
            string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase)));

    public Task<Result<AppUser>> CreateAsync(AppUser user, string password, CancellationToken cancellationToken)
    {
        if (CreateFailure is not null)
        {
            return Task.FromResult(Result.Failure<AppUser>(CreateFailure));
        }

        user.Id = _nextId++;

        _users.Add(user);
        _passwords[user.Id] = password;
        _roles[user.Id] = [];

        return Task.FromResult(Result.Success(user));
    }

    public Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken) =>
        Task.FromResult(_passwords.TryGetValue(user.Id, out var stored) && stored == password);

    public Task<IReadOnlyCollection<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<string>>(
            _roles.TryGetValue(user.Id, out var roles) ? roles : []);

    public Task<Result> AddToRoleAsync(AppUser user, string role, CancellationToken cancellationToken)
    {
        if (!_roles.TryGetValue(user.Id, out var roles))
        {
            roles = [];
            _roles[user.Id] = roles;
        }

        roles.Add(role);

        return Task.FromResult(Result.Success());
    }
}
