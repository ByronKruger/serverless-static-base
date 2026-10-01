namespace Platform.Common.Results;

/// <summary>
/// The kinds of expected failure a service method may return. Each one maps to a
/// single HTTP status code in <see cref="Http.ResultExtensions"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>Input the caller can fix. Maps to 400.</summary>
    Validation,

    /// <summary>The caller is not authenticated. Maps to 401.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed. Maps to 403.</summary>
    Forbidden,

    /// <summary>The resource does not exist (or is not visible). Maps to 404.</summary>
    NotFound,

    /// <summary>The request conflicts with current state. Maps to 409.</summary>
    Conflict
}
