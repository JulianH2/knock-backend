using System.Text.Json;

namespace Knock.ContractTest;

/// <summary>TT-CI-02 (KNOCK-137): los contratos publicados en <c>docs/</c> son OpenAPI 3.x válidos, todas sus operaciones declaran respuestas y los errores con cuerpo usan ProblemDetails.</summary>
public sealed class OpenApiContractTests
{
    private static readonly HashSet<string> _httpMethods = new(["get", "put", "post", "delete", "options", "head", "patch", "trace"], StringComparer.Ordinal);

    private static JsonDocument Load(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new InvalidOperationException("Knock.slnx was not found above the test output directory.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", fileName)));
    }

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> Operations(JsonElement root)
    {
        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var item in path.Value.EnumerateObject().Where(property => _httpMethods.Contains(property.Name)))
            {
                yield return (path.Name, item.Name, item.Value);
            }
        }
    }

    [Theory]
    [InlineData("openapi.v1.json")]
    [InlineData("openapi.identity.v1.json")]
    public void ContractFile_IsOpenApi3WithVersionAndPaths_TC_TT_CI_02_01(string fileName)
    {
        using var document = Load(fileName);
        var root = document.RootElement;

        Assert.StartsWith("3.", root.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("info").GetProperty("version").GetString()));
        var paths = root.GetProperty("paths").EnumerateObject().ToList();
        Assert.NotEmpty(paths);
    }

    [Theory]
    [InlineData("openapi.v1.json")]
    [InlineData("openapi.identity.v1.json")]
    public void EveryOperation_DeclaresAtLeastOneResponse_TC_TT_CI_02_02(string fileName)
    {
        using var document = Load(fileName);

        var withoutResponses = Operations(document.RootElement)
            .Where(operation => !operation.Operation.TryGetProperty("responses", out var responses)
                || responses.ValueKind != JsonValueKind.Object
                || !responses.EnumerateObject().Any())
            .Select(operation => $"{operation.Method.ToUpperInvariant()} {operation.Path}")
            .ToList();

        Assert.Empty(withoutResponses);
    }

    [Theory]
    [InlineData("openapi.v1.json")]
    [InlineData("openapi.identity.v1.json")]
    public void ErrorResponsesWithABody_UseProblemDetails_TC_TT_CI_02_03(string fileName)
    {
        using var document = Load(fileName);

        var errorsWithoutProblemDetails = Operations(document.RootElement)
            .SelectMany(operation => operation.Operation.GetProperty("responses").EnumerateObject()
                .Where(response => response.Name.Length > 0
                    && response.Name[0] is '4' or '5'
                    && response.Value.TryGetProperty("content", out var content)
                    && !content.GetRawText().Contains("ProblemDetails", StringComparison.Ordinal))
                .Select(response => $"{operation.Method.ToUpperInvariant()} {operation.Path} {response.Name}"))
            .ToList();

        Assert.Empty(errorsWithoutProblemDetails);
    }
}
