using System.Text.RegularExpressions;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Identity.Api.Modules.Usuarios.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Knock.Identity.Api.Modules.Usuarios.Services;

/// <summary>Carga las cuentas de demostración tras validar el destino.</summary>
internal sealed partial class DemoClientSeeder(
    UsuariosStore store, PhoneProtector phones, IProfileCache cache, ILogger<DemoClientSeeder> logger,
    IConfiguration configuration, IHostEnvironment environment)
{
    public async Task SeedAsync(string path, CancellationToken cancellationToken)
    {
        CheckEnvironment();
        await store.ValidateDemoAsync(cancellationToken);
        var input = BsonSerializer.Deserialize<BsonArray>(await File.ReadAllTextAsync(path, cancellationToken));
        var documents = new List<BsonDocument>();
        var ids = new HashSet<ObjectId>();
        var uids = new HashSet<string>(StringComparer.Ordinal);
        var phoneNumbers = new HashSet<string>(StringComparer.Ordinal);
        if (input.Count != 10)
        {
            throw new InvalidOperationException("Invalid demo seed file.");
        }

        foreach (var entry in input.Select(item => item.AsBsonDocument))
        {
            ValidateFields(entry);
            var id = entry["_id"].AsObjectId;
            var phone = entry["celular"].AsString;
            var uid = entry["uid"].AsString;
            if (!ids.Add(id) || !uids.Add(uid) || !phoneNumbers.Add(phone)
                || !Regex.IsMatch(uid, "^demo-cliente-[0-9]{2}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
                || !Regex.IsMatch(phone, "^\\+5200000[0-9]{5}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
                || string.IsNullOrWhiteSpace(entry["nombre"].AsString)
                || entry["estado"].AsString != "activa" || !entry["perfilCompleto"].AsBoolean
                || !entry["demo"].AsBoolean || entry["version"].AsInt32 != 1)
            {
                throw new InvalidOperationException("Invalid demo seed file.");
            }
            var encrypted = phones.Protect(phone, id.ToString());
            var hash = phones.Hash(phone);
            var document = entry.DeepClone().AsBsonDocument;
            document.Remove("uid");
            document.Remove("celular");
            document["sub"] = UserIdDerivation.Derive(entry["uid"].AsString).ToString("D");
            document["celularCifrado"] = new BsonBinaryData(encrypted.Ciphertext);
            document["celularCifradoVersion"] = encrypted.KeyVersion;
            document["celularHash"] = new BsonBinaryData(hash.Value);
            document["celularHashVersion"] = hash.KeyVersion;
            document["esTecnico"] = false;
            document["creadoEn"] = BsonDateTime.Create(DateTime.Parse(entry["creadoEn"].AsString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal));
            foreach (var consent in document["consentimientos"].AsBsonArray.Select(item => item.AsBsonDocument))
            {
                consent["aceptadoEn"] = BsonDateTime.Create(DateTime.Parse(consent["aceptadoEn"].AsString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal));
            }

            documents.Add(document);
        }

        foreach (var document in documents)
        {
            var conflict = await store.DemoConflictAsync(document, cancellationToken);
            if (conflict is not null)
            {
                throw new InvalidOperationException($"Demo seed conflict on {conflict} index.");
            }
        }

        foreach (var document in documents)
        {
            await store.UpsertDemoAsync(document, cancellationToken);
        }

        foreach (var document in documents)
        {
            if (!await DemoProfileCache.TryRemoveAsync(cache, document["sub"].AsString, cancellationToken))
            {
                LogCacheUnavailable(logger, document["_id"].AsObjectId.ToString());
            }
        }
    }

    [LoggerMessage(EventId = 2250, Level = LogLevel.Warning, Message = "Demo profile cache invalidation unavailable for demo document {DemoId}; a later load will retry")]
    private static partial void LogCacheUnavailable(ILogger logger, string demoId);

    private void CheckEnvironment()
    {
        if (!DemoSeedStartup.ShouldRun(configuration.GetValue<bool>("Seed:Demo"), environment.EnvironmentName))
        {
            throw new InvalidOperationException("Demo seed is disabled or the environment is Production.");
        }
    }

    private static void ValidateFields(BsonDocument entry)
    {
        string[] fields = ["_id", "uid", "celular", "nombre", "estado", "perfilCompleto", "creadoEn", "version", "demo", "consentimientos"];
        if (entry.ElementCount != fields.Length || entry.Names.Any(name => !fields.Contains(name, StringComparer.Ordinal)))
        {
            throw new InvalidOperationException("Invalid demo seed file.");
        }

        var name = entry["nombre"].AsString;
        ValidateUtc(entry["creadoEn"].AsString);
        if (Regex.IsMatch(name, "[0-9@]|https?://|www\\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        {
            throw new InvalidOperationException("Invalid demo seed file.");
        }

        var consents = entry["consentimientos"].AsBsonArray.Select(item => item.AsBsonDocument).ToArray();
        if (consents.Length != 2 || consents.Count(item => item["documento"] == "aviso_privacidad") != 1
            || consents.Count(item => item["documento"] == "terminos") != 1)
        {
            throw new InvalidOperationException("Invalid demo seed file.");
        }

        foreach (var consent in consents)
        {
            string[] names = ["documento", "version", "aceptadoEn", "deviceId"];
            if (consent.ElementCount != names.Length || consent.Names.Any(name => !names.Contains(name, StringComparer.Ordinal))
                || string.IsNullOrWhiteSpace(consent["version"].AsString) || string.IsNullOrWhiteSpace(consent["deviceId"].AsString))
            {
                throw new InvalidOperationException("Invalid demo seed file.");
            }

            ValidateUtc(consent["aceptadoEn"].AsString);
        }
    }

    private static void ValidateUtc(string value)
    {
        if (!value.EndsWith('Z', StringComparison.Ordinal) || !DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date) || date.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Invalid demo seed file.");
        }
    }
}
