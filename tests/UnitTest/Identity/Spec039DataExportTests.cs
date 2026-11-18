using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Services;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-06 (T-05): una exportación en curso por persona y la tarea que la prepara. Sin MongoDB.</summary>
public class Spec039DataExportTests
{
    private const string _userId = "665f1b2c3d4e5f6071829304";
    private const string _sub = "f0000000-0000-8000-8000-000000000001";

    private static (DataExportService Service, FakeAccountStore Store, Spec039Outbox Outbox) Build()
    {
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot(_userId, _sub, AccountState.Active, false, [1, 2, 3], null));
        var outbox = new Spec039Outbox();
        var service = new DataExportService(store, outbox, new Spec039Clock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero)), NullLogger<DataExportService>.Instance);
        return (service, store, outbox);
    }

    [Fact]
    public async Task WithoutAnExportInProgress_MarksItAndSchedulesTheTask_CA_039_14()
    {
        var (service, store, outbox) = Build();

        await service.RequestAsync(_sub, CancellationToken.None);

        store.ExportsInProgress.Should().Equal(_userId);
        outbox.ExportTasks.Should().Equal(_userId);
    }

    [Fact]
    public async Task ASecondRequestWhileTheFirstRuns_Is409ExportInProgressAndSchedulesNothingMore_CA_039_16()
    {
        var (service, _, outbox) = Build();
        await service.RequestAsync(_sub, CancellationToken.None);

        var act = () => service.RequestAsync(_sub, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<DomainException>()).Which;
        error.Kind.Should().Be(DomainErrorKind.Conflict);
        error.Code.Should().Be("export_in_progress");
        outbox.ExportTasks.Should().ContainSingle();
    }

    [Fact]
    public async Task RepeatedRequestFromABadConnection_NeverLeavesTwoExports_CA_039_43()
    {
        var (service, store, outbox) = Build();

        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try
            {
                await service.RequestAsync(_sub, CancellationToken.None);
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
        }));

        results.Count(started => started).Should().Be(1);
        outbox.ExportTasks.Should().ContainSingle();
        store.ExportsInProgress.Should().ContainSingle();
    }

    [Fact]
    public async Task IfTheTaskCannotBeScheduled_TheMarkIsRemovedSoThePersonIsNotBlocked_CA_039_14()
    {
        var (service, store, outbox) = Build();
        outbox.FailExport = true;

        var act = () => service.RequestAsync(_sub, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        store.ExportsInProgress.Should().BeEmpty();
    }

    [Fact]
    public async Task AccountThatNoLongerExists_Is404()
    {
        var (service, _, _) = Build();

        var act = () => service.RequestAsync("f0000000-0000-8000-8000-0000000000ff", CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Kind.Should().Be(DomainErrorKind.NotFound);
    }
}
