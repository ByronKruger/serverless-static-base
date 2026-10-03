namespace HopeTech.Common.Contracts;

/// <summary>Body of <c>POST /api/auth/login</c>.</summary>
/// <param name="Email">The address the account was registered with (D-0005).</param>
/// <param name="Password">Plain-text password, checked against the stored hash.</param>
public sealed record LoginRequest(string Email, string Password);
