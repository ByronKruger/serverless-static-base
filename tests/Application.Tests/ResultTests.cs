using Coffeeg.Api.Results;

namespace Coffeeg.Application.Tests;

/// <summary>
/// Guards the contract every service method depends on: a success carries a value
/// and no error, a failure carries an error and no value.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_exposes_its_value()
    {
        var result = Result.Success("flat white");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("flat white", result.Value);
    }

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_exposes_its_error()
    {
        var error = Error.NotFound("preference.not_found", "No preference for that beverage type.");

        Result<string> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("preference.not_found", result.Error.Code);
    }

    [Fact]
    public void Failure_has_no_value()
    {
        Result<string> result = Error.Conflict("preference.duplicate", "One preference per beverage type.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_converts_implicitly_to_a_success()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }
}
