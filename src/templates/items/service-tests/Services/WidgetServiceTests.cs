using AutoMapper;
using HopeTech.Api.Dtos;
using HopeTech.Api.Services;
using HopeTech.Api.Tests.Fakes;
using HopeTech.Common.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace HopeTech.Api.Tests.Services;

/// <summary>
/// The service in isolation. Fake ports only: no <c>WebApplicationFactory</c>, no
/// <c>DbContext</c>, no database. If a test here needs any of those, the service is not
/// isolated and the dependency belongs behind a port.
///
/// The mapper is real rather than substituted, so the profile is exercised too - and
/// a hand-written fake beats a mocking framework for a port this small, because it
/// reads better and it fails louder.
///
/// The HTTP half of the contract - status codes, the problem+json shape, binding - is
/// not tested here. That is the integration suite's job, over the real pipeline.
/// </summary>
public sealed class WidgetServiceTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    private WidgetService CreateSubject()
    {
        // Add the profile once this resource has one: configuration.AddProfile<WidgetProfile>().
        IMapper mapper = new MapperConfiguration(
                configuration => { },
                NullLoggerFactory.Instance)
            .CreateMapper();

        return new WidgetService(mapper, _clock, _currentUser);
    }

    [Fact]
    public async Task GetAsync_returns_not_found_when_no_widget_has_that_id()
    {
        var result = await CreateSubject().GetAsync(id: 404, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        // The code is what a client branches on, so it is the part worth asserting.
        Assert.Equal("widget.not_found", result.Error.Code);
    }

    [Fact]
    public async Task ListAsync_returns_an_empty_list_when_nothing_exists()
    {
        var result = await CreateSubject().ListAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task CreateAsync_returns_conflict_when_the_name_is_taken()
    {
        var request = new CreateWidgetRequest("Taken", Description: null);

        var result = await CreateSubject().CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
