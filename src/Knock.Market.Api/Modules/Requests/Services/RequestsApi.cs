namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class RequestsApi : IRequestsApi
{
    public string ModuleName => RequestsModuleExtensions.ModuleKey;

    public bool CanArrive(string state, string modality, string day, bool blocked, bool codeUsed, DateTime? expiresAtUtc, DateTime nowUtc) =>
        Domain.VisitRules.CanArrive(new Domain.VisitState(state, modality, day, blocked, codeUsed, expiresAtUtc), nowUtc);
}
