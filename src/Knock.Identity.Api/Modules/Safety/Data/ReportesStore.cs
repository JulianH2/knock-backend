using Knock.Identity.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Safety.Data;

/// <summary>Un reporte tal como lo necesita el servicio: nunca lleva el texto ni la evidencia al leerlo para listar (spec 043, R-22).</summary>
internal sealed record StoredReport(ObjectId Id, string ReporterId, string ReportedId, string Reason, string State, bool HasDecision, bool HasAppeal, DateTime CreatedAtUtc);

/// <summary>Lo que hace falta para guardar un reporte nuevo.</summary>
internal sealed record NewReport(string ReporterId, string ReportedId, string? ReferenceKind, string? ReferenceId, string Reason, string Text, IReadOnlyList<string> Evidence, DateTime CreatedAtUtc);

/// <summary>
/// Colección <c>reportes</c> (spec 043, §6). Es el único acceso a MongoDB de los reportes. La persona que reporta y la reportada se guardan con su identificador público (el <c>sub</c>).
/// Toda lectura de la lista lleva el <c>sub</c> de quien llama (R-24). El texto y la evidencia no salen de aquí hacia quien llama: solo los lee el panel (R-22).
/// </summary>
internal sealed class ReportesStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
{
    public const string ReportsCollection = "reportes";
    private const string _reporterField = "reportaId";
    private const string _reportedField = "reportadoId";
    private const string _stateField = "estado";
    private const string _decisionField = "decision";
    private const string _appealField = "apelacion";
    private const string _createdField = "creadoEn";
    private const string _idField = "_id";

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Reports => database.GetCollection<BsonDocument>(ReportsCollection);

    /// <summary>Índices de la spec 043 (§6). Idempotente; lo usan las pruebas y el script de <c>db/</c> hace lo mismo.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Reports.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("gravedad").Ascending(_stateField).Ascending(_createdField), new CreateIndexOptions { Name = "gravedad_1_estado_1_creadoEn_1" }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(_reportedField), new CreateIndexOptions { Name = "reportadoId_1" }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(_reporterField), new CreateIndexOptions { Name = "reportaId_1" }),
            ],
            cancellationToken);

    /// <summary>El reporte abierto de esa persona contra la misma otra por el mismo motivo, si ya existe: una repetición por mala señal no abre otro.</summary>
    public async Task<StoredReport?> FindOpenDuplicateAsync(string reporterId, string reportedId, string reason, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var document = await Reports
            .Find(filters.Eq(_reporterField, reporterId) & filters.Eq(_reportedField, reportedId) & filters.Eq("motivo", reason) & filters.Eq(_stateField, "abierto"), _findOptions)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToStored(document);
    }

    public async Task<StoredReport> InsertAsync(NewReport report, CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            [_idField] = ObjectId.GenerateNewId(),
            [_reporterField] = report.ReporterId,
            [_reportedField] = report.ReportedId,
            ["motivo"] = report.Reason,
            ["descripcion"] = report.Text,
            ["evidencia"] = new BsonArray(report.Evidence),
            [_stateField] = "abierto",
            [_createdField] = new BsonDateTime(report.CreatedAtUtc),
        };
        if (report.ReferenceKind is not null)
        {
            document["referencia"] = new BsonDocument { ["tipo"] = report.ReferenceKind, ["id"] = report.ReferenceId };
        }

        await Reports.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToStored(document);
    }

    /// <summary>Los reportes hechos por quien llama, del más reciente al más antiguo (por <c>_id</c>). Pide uno de más para saber si hay otra página.</summary>
    public async Task<IReadOnlyList<StoredReport>> ListByReporterAsync(string reporterId, ObjectId? after, int take, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var filter = filters.Eq(_reporterField, reporterId);
        if (after is { } cursor)
        {
            filter &= filters.Lt(_idField, cursor);
        }

        var documents = await Reports.Find(filter, _findOptions).Sort(Builders<BsonDocument>.Sort.Descending(_idField)).Limit(take).ToListAsync(cancellationToken);
        return [.. documents.Select(ToStored)];
    }

    public async Task<StoredReport?> FindAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var document = await Reports.Find(Builders<BsonDocument>.Filter.Eq(_idField, id), _findOptions).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToStored(document);
    }

    /// <summary>
    /// Guarda la apelación en una sola operación condicionada a que haya decisión y no haya apelación previa: de dos apelaciones simultáneas gana una. Falso si no se aplicó.
    /// </summary>
    public async Task<bool> AppealAsync(ObjectId id, string text, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var result = await Reports.UpdateOneAsync(
            filters.Eq(_idField, id) & filters.Eq(_stateField, "decidido") & filters.Exists(_decisionField) & filters.Exists(_appealField, exists: false),
            Builders<BsonDocument>.Update
                .Set(_appealField, new BsonDocument { ["texto"] = text, [_createdField] = new BsonDateTime(nowUtc) })
                .Set(_stateField, "en_apelacion"),
            cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    private static StoredReport ToStored(BsonDocument d) => new(
        d[_idField].AsObjectId,
        d[_reporterField].AsString,
        d[_reportedField].AsString,
        d["motivo"].AsString,
        d[_stateField].AsString,
        d.Contains(_decisionField),
        d.Contains(_appealField),
        d[_createdField].ToUniversalTime());
}
