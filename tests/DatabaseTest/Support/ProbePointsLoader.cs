using System.Text.Json;

namespace Knock.DatabaseTest.Support;

internal static class ProbePointsLoader
{
    private static List<(string Colonia, double Longitude, double Latitude)> Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Knock.slnx was not found above the test output directory.");
        }

        var path = Path.Combine(directory.FullName, "tests", "Fixtures", "catalog", "probe-points.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var list = new List<(string Colonia, double Longitude, double Latitude)>(document.RootElement.GetArrayLength());
        foreach (var element in document.RootElement.EnumerateArray())
        {
            list.Add((
                element.GetProperty("colonia").GetString()!,
                element.GetProperty("longitude").GetDouble(),
                element.GetProperty("latitude").GetDouble()));
        }

        return list;
    }

    public static IReadOnlyList<(string Colonia, double Longitude, double Latitude)> LoadLongLat() => Load();

    public static IReadOnlyList<(string Colonia, double Lng, double Lat)> LoadLngLat()
    {
        var points = Load();
        var list = new List<(string Colonia, double Lng, double Lat)>(points.Count);
        foreach (var point in points)
        {
            list.Add((point.Colonia, point.Longitude, point.Latitude));
        }
        return list;
    }
}
