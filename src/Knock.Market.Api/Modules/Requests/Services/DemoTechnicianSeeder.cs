using Knock.Market.Api.Modules.Requests.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class DemoTechnicianSeeder(DemoTechnicianStore store, IConfiguration configuration, IHostEnvironment environment)
{
    private const string _contactField = "contactoPublico";

    private static readonly string[] _fields = ["_id", "uid", "usuarioId", "nombre", "estado", "oculto", "oficios", "coloniaBaseId", "ubicacionBase", "radioAtencionKm", "version", "demo"];

    public async Task SeedAsync(string path, CancellationToken cancellationToken)
    {
        if (!DemoTechnicianStartup.ShouldRun(configuration.GetValue<bool>("Seed:Demo"), environment.EnvironmentName))
            throw new InvalidOperationException("Demo seed is disabled or the environment is Production.");
        await store.ValidateAsync(cancellationToken);
        var input = BsonSerializer.Deserialize<BsonArray>(await File.ReadAllTextAsync(path, cancellationToken));
        var documents = input.Select(x => x.AsBsonDocument).ToArray();
        Validate(documents);
        await store.SaveAsync(documents, cancellationToken);
    }

    private static void Validate(BsonDocument[] documents)
    {
        var ids = new HashSet<ObjectId>();
        var users = new HashSet<string>(StringComparer.Ordinal);
        var uids = new HashSet<string>(StringComparer.Ordinal);
        if (documents.Length != 4) throw new InvalidOperationException("Invalid demo technician seed.");
        foreach (var doc in documents)
        {
            var hasContact = doc.Contains(_contactField);
            if (doc.ElementCount != _fields.Length + (hasContact ? 1 : 0)
                || doc.Names.Any(x => x != _contactField && !_fields.Contains(x, StringComparer.Ordinal))
                || (hasContact && !ValidContact(doc[_contactField]))
                || !ids.Add(doc["_id"].AsObjectId) || !users.Add(doc["usuarioId"].AsString) || !uids.Add(doc["uid"].AsString)
                || !Guid.TryParseExact(doc["usuarioId"].AsString, "D", out _)
                || !doc["uid"].AsString.StartsWith("demo-tecnico-", StringComparison.Ordinal)
                || !doc["nombre"].AsString.StartsWith("Demo ", StringComparison.Ordinal)
                || doc["estado"] != "activo" || doc["oculto"].AsBoolean || !doc["demo"].AsBoolean || doc["version"] != 1
                || !ObjectId.TryParse(doc["coloniaBaseId"].AsString, out _)
                || doc["radioAtencionKm"].AsInt32 is < 1 or > 30 || doc["oficios"].AsBsonArray.Count == 0
                || doc["oficios"].AsBsonArray.Any(x => !x.IsString || x.AsString is not ("ele" or "mec" or "plo"))
                || !ValidPoint(doc["ubicacionBase"].AsBsonDocument))
                throw new InvalidOperationException("Invalid demo technician seed.");
        }
    }

    /// <summary>Contacto público ficticio (spec 021, R-15): solo un teléfono del rango de demostración 55000000NN, nunca un número real.</summary>
    private static bool ValidContact(BsonValue value) =>
        value is BsonDocument { ElementCount: 1 } contact
        && contact.TryGetValue("telefono", out var phone)
        && phone.IsString
        && phone.AsString.Length == 10
        && phone.AsString.StartsWith("55000000", StringComparison.Ordinal)
        && phone.AsString.All(char.IsAsciiDigit);

    private static bool ValidPoint(BsonDocument point)
    {
        if (point.ElementCount != 2 || point.GetValue("type", "") != "Point" || !point.TryGetValue("coordinates", out var value)
            || !value.IsBsonArray || value.AsBsonArray.Count != 2 || value.AsBsonArray.Any(x => !x.IsNumeric)) return false;
        var coords = value.AsBsonArray;
        return coords[0].ToDouble() is >= -180 and <= 180 && coords[1].ToDouble() is >= -90 and <= 90;
    }
}
