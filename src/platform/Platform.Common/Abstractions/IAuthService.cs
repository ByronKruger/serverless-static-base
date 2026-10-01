using Platform.Common.Contracts;
using Platform.Common.Results;

namespace Platform.Common.Abstractions;

/// <summary>
/// Registration and sign-in. One public method per use case; expected failures come
/// back as <see cref="Result"/> failures, never exceptions.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates an account and signs the new person in. Fails with a conflict when the
    /// email or username is taken, and with a validation failure when the password does
    /// not meet the policy.
    /// </summary>
    Task<Result<AuthenticationResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Signs an existing person in with their email and password. Fails as unauthorized
    /// for both an unknown address and a wrong password, so the response cannot be used
    /// to enumerate accounts.
    /// </summary>
    Task<Result<AuthenticationResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
