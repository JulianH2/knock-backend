using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Assistant.Data;

internal sealed record UsageRecord(string UserId, string Function, string Model, string PromptVersion, int InputTokens, int OutputTokens, int LatencyMs, string Origin, string? FallbackReason, bool Truncated, DateTime CreatedAt);

internal sealed class UsoIaStore(IMongoDatabase database)
{
    public Task SaveAsync(UsageRecord usage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usage);
        var doc = new BsonDocument
        {
            ["usuarioId"] = usage.UserId,
            ["funcion"] = usage.Function,
            ["modelo"] = usage.Model,
            ["versionPrompt"] = usage.PromptVersion,
            ["tokensEntrada"] = usage.InputTokens,
            ["tokensSalida"] = usage.OutputTokens,
            ["latenciaMs"] = usage.LatencyMs,
            ["origen"] = usage.Origin,
            ["recortado"] = usage.Truncated,
            ["creadoEn"] = usage.CreatedAt,
        };
        if (usage.FallbackReason is not null)
        {
            doc["motivoRespaldo"] = usage.FallbackReason;
        }

        return database.GetCollection<BsonDocument>("uso_ia").InsertOneAsync(doc, cancellationToken: cancellationToken);
    }
}
