using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Services;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-09 (T-08): conteos e identificadores propios, con tope de 50, y falla cerrada si Mercado no responde. Sin red.</summary>
public class Spec039PendingTests
{
    private const string _sub = "f0000000-0000-8000-8000-000000000001";

    private static IReadOnlyList<string> Ids(int count) => [.. Enumerable.Range(1, count).Select(i => i.ToString("x24", System.Globalization.CultureInfo.InvariantCulture))];

    [Fact]
    public async Task ReturnsTheCountsAndIdsOfTheOpenAppointmentsAndQuotes_CA_039_37()
    {
        var market = new Spec039Market { Pending = new AccountPending(2, Ids(2), 3, Ids(3), false) };

        var result = await new PendingService(market).ListAsync(_sub, CancellationToken.None);

        result.AppointmentTotal.Should().Be(2);
        result.AppointmentIds.Should().Equal(Ids(2));
        result.QuoteTotal.Should().Be(3);
        result.QuoteIds.Should().Equal(Ids(3));
    }

    [Fact]
    public async Task WithNothingOpen_ReturnsEmptyGroups_CA_039_37()
    {
        var result = await new PendingService(new Spec039Market()).ListAsync(_sub, CancellationToken.None);

        result.Should().Be(new AccountPending(0, [], 0, [], false));
    }

    [Fact]
    public async Task OverFifty_KeepsTheTotalAndCapsTheIds_CA_039_37()
    {
        var market = new Spec039Market { Pending = new AccountPending(80, Ids(80), 51, Ids(51), false) };

        var result = await new PendingService(market).ListAsync(_sub, CancellationToken.None);

        result.AppointmentIds.Should().HaveCount(50);
        result.AppointmentTotal.Should().Be(80);
        result.QuoteIds.Should().HaveCount(50);
        result.QuoteTotal.Should().Be(51);
    }

    [Fact]
    public async Task ATotalSmallerThanTheIdsMarketSent_NeverUnderstatesThem_CA_039_37()
    {
        var market = new Spec039Market { Pending = new AccountPending(0, Ids(2), 0, [], false) };

        var result = await new PendingService(market).ListAsync(_sub, CancellationToken.None);

        result.AppointmentTotal.Should().Be(2);
    }

    [Fact]
    public async Task OnlyTheCallersSubIsSentToMarket_CA_039_40()
    {
        var market = new RecordingMarket();

        await new PendingService(market).ListAsync(_sub, CancellationToken.None);

        market.Subs.Should().Equal(_sub);
    }

    [Fact]
    public async Task WithoutTheInternalRoutes_TheDefaultClientFailsClosedWith503MarketUnavailable_CA_039_23()
    {
        var act = () => new PendingService(new UnavailableMarketAccountClient()).ListAsync(_sub, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<DomainException>()).Which;
        error.Kind.Should().Be(DomainErrorKind.ServiceUnavailable);
        error.Code.Should().Be("market_unavailable");
    }

    private sealed class RecordingMarket : IMarketAccountClient
    {
        public List<string> Subs { get; } = [];

        public Task<AccountPending> GetPendingAsync(string sub, CancellationToken cancellationToken)
        {
            Subs.Add(sub);
            return Task.FromResult(new AccountPending(0, [], 0, [], false));
        }

        public Task CancelOpenAsync(string sub, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
