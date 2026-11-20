using Knock.Identity.Api.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>
/// <c>panel_auditoria</c> (spec 033, §6, R-23): de solo inserción para el servicio. Esta clase no expone lectura, actualización ni borrado, y una prueba de
/// arquitectura lo vigila. Los documentos vencen a los 365 días (índice TTL). Solo se escriben los campos de <see cref="PanelAuditEntry"/>.
/// </summary>
internal sealed class PanelAuditoriaStore([FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database) : IPanelAuditoriaStore
{
    public const string Collection = "panel_auditoria";

    private static readonly TimeSpan _retention = TimeSpan.FromDays(365);

    private IMongoCollection<BsonDocument> Entries { get; } = database.GetCollection<BsonDocument>(Collection);

    /// <summary>Índices de §6 (idempotentes); los invocan las pruebas y el script <c>db/044-identity-panel.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Entries.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending("fecha"), new CreateIndexOptions { ExpireAfter = _retention, Name = "fecha_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending("sub").Descending("fecha"), new CreateIndexOptions { Name = "sub_1_fecha_-1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending("evento").Descending("fecha"), new CreateIndexOptions { Name = "evento_1_fecha_-1" }),
            ],
            cancellationToken);
    }

    public async Task InsertAsync(PanelAuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var document = new BsonDocument
        {
            { "fecha", new BsonDateTime(entry.Fecha) },
            { "tipo", entry.Tipo },
            { "evento", entry.Evento },
            { "resultado", entry.Resultado },
            { "ipHmac", entry.IpHmac },
            { "correlationId", entry.CorrelationId },
        };
        Optional(document, "sub", entry.Sub);
        Optional(document, "sid", entry.Sid);
        Optional(document, "permiso", entry.Permiso);
        Optional(document, "ruta", entry.Ruta);
        Optional(document, "recursoId", entry.RecursoId);
        await Entries.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    private static void Optional(BsonDocument document, string name, string? value)
    {
        if (value is not null)
        {
            document[name] = value;
        }
    }
}
