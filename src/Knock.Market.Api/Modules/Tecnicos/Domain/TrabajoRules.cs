namespace Knock.Market.Api.Modules.Tecnicos.Domain;

internal static class TrabajoRules
{
    public static bool CanShowAddress(string? state) => state is "confirmada" or "en_curso";

    public static string? RejectionReason(string? state, string? reason) =>
        state is "rechazada" or "retirada" ? reason : null;

    public static int CompareFinished(DateTime leftDate, string leftId, DateTime rightDate, string rightId)
    {
        var byDate = rightDate.CompareTo(leftDate);
        return byDate != 0 ? byDate : string.CompareOrdinal(rightId, leftId);
    }
}
