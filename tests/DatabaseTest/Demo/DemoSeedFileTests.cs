using System.Globalization;
using System.Text.Json;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.DatabaseTest.Demo;

public sealed class DemoSeedFileTests
{
    private static readonly string _seedPath = Path.Combine(CatalogTestDatabase.SeedsDirectory, "demo", "identity", "usuarios.json");

    [Fact]
    public void FormatAndIds_CA_038_01()
    {
        using var seed = JsonDocument.Parse(File.ReadAllText(_seedPath));
        Assert.Equal(JsonValueKind.Array, seed.RootElement.ValueKind);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(CatalogTestDatabase.SeedsDirectory, "catalog"), "*.json"))
        {
            using var catalog = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var entry in catalog.RootElement.EnumerateArray())
            {
                Assert.True(ids.Add(entry.GetProperty("_id").GetProperty("$oid").GetString()!));
            }
        }

        foreach (var entry in seed.RootElement.EnumerateArray())
        {
            var id = entry.GetProperty("_id");
            Assert.Single(id.EnumerateObject());
            var oid = id.GetProperty("$oid").GetString();
            Assert.Matches("^[0-9a-fA-F]{24}$", oid!);
            Assert.True(ids.Add(oid!));
            CheckFieldNames(entry);
            Assert.Equal(1, entry.GetProperty("version").GetInt32());
            Assert.True(entry.GetProperty("demo").GetBoolean());
            CheckUtc(entry.GetProperty("creadoEn").GetString());
        }
    }

    [Fact]
    public void TenClients_CA_038_02()
    {
        using var seed = JsonDocument.Parse(File.ReadAllText(_seedPath));
        Assert.Equal(10, seed.RootElement.GetArrayLength());
        var uids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in seed.RootElement.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("nombre").GetString()));
            var uid = entry.GetProperty("uid").GetString();
            Assert.Matches("^demo-cliente-[0-9]{2}$", uid!);
            Assert.True(uids.Add(uid!));
            Assert.False(entry.TryGetProperty("esTecnico", out var technician) && technician.GetBoolean());
            Assert.False(entry.TryGetProperty("solicitudes", out _));
        }
    }

    [Fact]
    public void FictitiousData_CA_038_03()
    {
        using var seed = JsonDocument.Parse(File.ReadAllText(_seedPath));
        var phones = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in seed.RootElement.EnumerateArray())
        {
            var phone = entry.GetProperty("celular").GetString();
            Assert.Matches("^\\+5200000[0-9]{5}$", phone!);
            Assert.True(phones.Add(phone!));
            Assert.DoesNotMatch("[0-9@]|https?://|www\\.", entry.GetProperty("nombre").GetString()!);
            CheckAllowedFields(entry);
        }
    }

    [Fact]
    public void DemoAccountState_CA_038_13()
    {
        var root = Directory.GetParent(Directory.GetParent(CatalogTestDatabase.SeedsDirectory)!.FullName)!.FullName;
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "Knock.Identity.Api", "appsettings.json")));
        var versions = config.RootElement.GetProperty("Consentimientos");
        using var seed = JsonDocument.Parse(File.ReadAllText(_seedPath));
        foreach (var entry in seed.RootElement.EnumerateArray())
        {
            Assert.Equal("activa", entry.GetProperty("estado").GetString());
            Assert.True(entry.GetProperty("perfilCompleto").GetBoolean());
            var consents = entry.GetProperty("consentimientos").EnumerateArray().ToArray();
            Assert.Equal(2, consents.Length);
            CheckConsent(consents, "aviso_privacidad", versions.GetProperty("AvisoPrivacidad").GetString());
            CheckConsent(consents, "terminos", versions.GetProperty("Terminos").GetString());
        }
    }

    private static void CheckConsent(JsonElement[] consents, string document, string? version)
    {
        var consent = Assert.Single(consents, item => item.GetProperty("documento").GetString() == document);
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.Equal(version, consent.GetProperty("version").GetString());
        CheckUtc(consent.GetProperty("aceptadoEn").GetString());
        Assert.False(string.IsNullOrWhiteSpace(consent.GetProperty("deviceId").GetString()));
    }

    private static void CheckUtc(string? value)
    {
        Assert.NotNull(value);
        Assert.EndsWith("Z", value, StringComparison.Ordinal);
        Assert.True(DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date));
        Assert.Equal(TimeSpan.Zero, date.Offset);
    }

    private static void CheckFieldNames(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                CheckFieldNames(item);
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                Assert.Matches("^(?:_id|\\$oid|[a-z][a-zA-Z0-9]*)$", property.Name);
                CheckFieldNames(property.Value);
            }
        }
    }

    private static void CheckAllowedFields(JsonElement entry)
    {
        string[] allowed = ["_id", "uid", "celular", "nombre", "estado", "perfilCompleto", "creadoEn", "version", "demo", "consentimientos"];
        string[] consentFields = ["documento", "version", "aceptadoEn", "deviceId"];
        foreach (var property in entry.EnumerateObject())
        {
            Assert.Contains(property.Name, allowed);
        }

        foreach (var consent in entry.GetProperty("consentimientos").EnumerateArray())
        {
            foreach (var property in consent.EnumerateObject())
            {
                Assert.Contains(property.Name, consentFields);
            }
        }
    }
}
