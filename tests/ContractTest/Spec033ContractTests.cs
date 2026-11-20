using System.Text.Json;

namespace Knock.ContractTest;

/// <summary>Spec 033, T-01: el contrato de Identity publica los cuatro endpoints de sesión del panel, la activación, la cookie y los códigos (§5).</summary>
public sealed class Spec033ContractTests
{
    private const string _session = "/api/v{version}/panel/sesion/";

    private static JsonDocument Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new InvalidOperationException("Knock.slnx was not found above the test output directory.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "openapi.identity.v1.json")));
    }

    private static JsonElement Operation(JsonDocument document, string path) =>
        document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("post");

    private static string[] Statuses(JsonElement operation) =>
        [.. operation.GetProperty("responses").EnumerateObject().Select(response => response.Name).Order(StringComparer.Ordinal)];

    [Theory]
    [InlineData("celular", "200,400,401,403,413,422,429,503")]
    [InlineData("totp", "200,400,401,403,413,422,429,503")]
    [InlineData("renovar", "200,401,403,429,503")]
    [InlineData("cerrar", "204,401,403,429")]
    public void Los_Cuatro_Endpoints_De_Sesion_Declaran_Sus_Respuestas_CA_033_02(string endpoint, string statuses)
    {
        using var document = Load();

        Assert.Equal(statuses.Split(','), Statuses(Operation(document, _session + endpoint)));
    }

    [Theory]
    [InlineData("totp", "200")]
    [InlineData("renovar", "200")]
    [InlineData("cerrar", "204")]
    public void La_Cookie_De_Renovacion_Esta_En_El_Contrato_CA_033_01(string endpoint, string status)
    {
        using var document = Load();

        var header = Operation(document, _session + endpoint).GetProperty("responses").GetProperty(status).GetProperty("headers").GetProperty("Set-Cookie");
        var description = header.GetProperty("description").GetString();
        Assert.Contains("__Secure-knock-panel-rt", description, StringComparison.Ordinal);
        Assert.Contains("HttpOnly", description, StringComparison.Ordinal);
        Assert.Contains("Partitioned", description, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("renovar")]
    [InlineData("cerrar")]
    public void El_Encabezado_X_Knock_Panel_Es_Obligatorio_En_Renovar_Y_Cerrar_CA_033_12(string endpoint)
    {
        using var document = Load();

        var parameters = Operation(document, _session + endpoint).GetProperty("parameters").EnumerateArray().ToArray();
        var marker = Assert.Single(parameters, parameter => parameter.GetProperty("name").GetString() == "X-Knock-Panel");
        Assert.Equal("header", marker.GetProperty("in").GetString());
        Assert.True(marker.GetProperty("required").GetBoolean());
    }

    [Fact]
    public void La_Activacion_Del_Totp_Esta_En_El_Contrato_CA_033_27()
    {
        using var document = Load();

        var operation = Operation(document, "/api/v{version}/panel/activacion");
        Assert.Contains("410", Statuses(operation));
        Assert.Contains("200", Statuses(operation));
    }
}
