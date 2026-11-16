using AwesomeAssertions;
using Knock.Contracts.Moderation;
using Knock.Market.Api.Modules.Media;
using Knock.Market.Api.Modules.Media.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Media;

public class MediaModuleRegistrationTests
{
    [Fact]
    public async Task AddMediaModule_DevelopmentWithoutModerator_FailsClosed_CA_013_05()
    {
        var services = new ServiceCollection();
        services.AddMediaModule(isDevelopment: true);
        using var provider = services.BuildServiceProvider();
        var moderator = provider.GetRequiredService<IContentModerator>();
        (await moderator.ReviewTextAsync("example", CancellationToken.None)).Outcome.Should().Be(ModerationOutcome.Unavailable);
        (await moderator.ReviewImageAsync(new byte[] { 1 }, CancellationToken.None)).Outcome.Should().Be(ModerationOutcome.Unavailable);
    }

    [Fact]
    public void AddMediaModule_ProductionWithoutModerator_DoesNotRegisterFallback_OI2_01()
    {
        var services = new ServiceCollection();
        services.AddMediaModule();
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IContentModerator));
    }

    [Fact]
    public void AddMediaModule_ExistingModerator_DoesNotReplaceIt_OI2_01()
    {
        var services = new ServiceCollection();
        var moderator = new TestModerator();
        services.AddSingleton<IContentModerator>(moderator);
        services.AddMediaModule(isDevelopment: true);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IContentModerator>().Should().BeSameAs(moderator);
    }

    private sealed class TestModerator : IContentModerator
    {
        public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(ModerationOutcome.Allowed));

        public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(ModerationOutcome.Allowed));
    }

    [Fact]
    public void AddMediaModule_RegistersTimeProvider_OI1_01()
    {
        var services = new ServiceCollection();
        services.AddMediaModule();
        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<TimeProvider>().Should().Be(TimeProvider.System);
    }

    [Fact]
    public void ZeroReducedMaxSide_FailsOnStart_OI1_03()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:ReducedMaxSide"] = "0" })
            .Build());
        services.AddMediaModule();
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<MediaOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
