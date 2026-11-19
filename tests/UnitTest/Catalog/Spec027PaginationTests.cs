using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Domain;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class Spec027PaginationTests
{
    private static readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private static readonly SearchQuery _query = new("boiler", "colonia-1", null, false, 1);

    [Fact]
    public void Orden_Determinista_CA_027_08()
    {
        SearchRank[] ranks =
        [
            new("7", 2, true, 5, 1), new("6", 1, true, 5, 1),
            new("4", 0, false, 5, 1), new("3", 0, true, 4, 2),
            new("2", 0, true, 4, 1), new("1", 0, true, 4, 1),
            new("5", 0, true, 5, 20),
        ];
        var expected = new[] { "5", "1", "2", "3", "4", "6", "7" };
        OrdenDeResultados.Order(ranks).Select(x => x.Id).Should().Equal(expected);
        OrdenDeResultados.Order(ranks.Reverse()).Select(x => x.Id).Should().Equal(expected);
    }

    [Fact]
    public void Grupos_Priorizan_Nombre_Y_Cobertura_CA_027_08()
    {
        OrdenDeResultados.Classify(true, true, true).Should().Be(0);
        OrdenDeResultados.Classify(false, true, true).Should().Be(1);
        OrdenDeResultados.Classify(true, false, true).Should().Be(2);
        OrdenDeResultados.Classify(false, false, true).Should().BeNull();
        OrdenDeResultados.Classify(false, true, false).Should().BeNull();
    }

    [Theory]
    [InlineData(45, 45, 3)]
    [InlineData(130, 100, 5)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 21, 2)]
    public void Cursor_Y_Tope_De_Cien_CA_027_10(int total, int count, int pages)
    {
        var ranks = Enumerable.Range(0, total).Select(i => new SearchRank(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), 0, false, 0, 1)).ToArray();
        var cursor = (string?)null;
        var received = new List<string>();
        var iterations = 0;
        do
        {
            var page = CursorDeBusqueda.Page(ranks, _query, 20, cursor, _key);
            received.AddRange(page.Items.Select(x => x.Id));
            cursor = page.NextCursor;
            iterations++;
        }
        while (cursor is not null && iterations < 10);
        received.Should().HaveCount(count).And.OnlyHaveUniqueItems();
        iterations.Should().Be(pages);
        cursor.Should().BeNull();
    }

    [Fact]
    public void Rechaza_Cursor_Alterado_CA_027_11()
    {
        var ranks = Enumerable.Range(0, 30).Select(i => new SearchRank(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), 0, true, 4, 2)).ToArray();
        var cursor = CursorDeBusqueda.Page(ranks, _query, 20, null, _key).NextCursor!;
        var corrupted = (cursor[0] == 'A' ? "B" : "A") + cursor[1..];
        Action tampered = () => CursorDeBusqueda.Page(ranks, _query, 20, corrupted, _key);
        Action otherQuery = () => CursorDeBusqueda.Page(ranks, _query with { Query = "frenos" }, 20, cursor, _key);
        Action otherVersion = () => CursorDeBusqueda.Page(ranks, _query with { DictionaryVersion = 2 }, 20, cursor, _key);
        Action otherKey = () => CursorDeBusqueda.Page(ranks, _query, 20, cursor, RandomNumberGenerator.GetBytes(32));
        tampered.Should().Throw<FormatException>();
        otherQuery.Should().Throw<FormatException>();
        otherVersion.Should().Throw<FormatException>();
        otherKey.Should().Throw<FormatException>();
        Encoding.UTF8.GetString(Convert.FromBase64String(cursor.Replace('-', '+').Replace('_', '/') + new string('=', (4 - cursor.Length % 4) % 4)))
            .Should().NotContain("019").And.NotContain("boiler").And.NotContain("colonia-1");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(19)]
    public void Limite_Acumulado_Con_Tamanos_Distintos_CA_027_10(int limit)
    {
        var ranks = Enumerable.Range(0, 130).Select(i => new SearchRank(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), 1, false, 0, null)).ToArray();
        var cursor = (string?)null;
        var received = new List<string>();
        do
        {
            var page = CursorDeBusqueda.Page(ranks, _query, limit, cursor, _key);
            received.AddRange(page.Items.Select(x => x.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);
        received.Should().HaveCount(100).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void Cursor_Liga_Todos_Los_Filtros_CA_027_11()
    {
        var ranks = Enumerable.Range(0, 3).Select(i => new SearchRank(i.ToString(System.Globalization.CultureInfo.InvariantCulture), 0, false, 0, null)).ToArray();
        var token = CursorDeBusqueda.Page(ranks, _query, 1, null, _key).NextCursor!;
        foreach (var different in new[] { _query with { ColoniaId = "otra" }, _query with { Trade = "plo" }, _query with { VerifiedOnly = true } })
        {
            Action act = () => CursorDeBusqueda.Page(ranks, different, 1, token, _key);
            act.Should().Throw<FormatException>();
        }
        CursorDeBusqueda.Page(ranks, _query with { Query = " Bóiler!! " }, 1, token, _key).Items.Single().Id.Should().Be("1");
        foreach (var invalid in new[] { "", "abc", token + "=", new string('A', 2049), "!" + token })
        {
            Action act = () => CursorDeBusqueda.Page(ranks, _query, 1, invalid, _key);
            act.Should().Throw<FormatException>();
        }
    }

    [Fact]
    public void Cambiar_Limite_No_Elude_Tope_CA_027_10()
    {
        var ranks = Enumerable.Range(0, 130).Select(i => new SearchRank(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), 0, false, 0, null)).ToArray();
        var cursor = (string?)null;
        var count = 0;
        var pageNumber = 0;
        do
        {
            var page = CursorDeBusqueda.Page(ranks, _query, pageNumber++ % 2 == 0 ? 7 : 20, cursor, _key);
            count += page.Items.Count;
            cursor = page.NextCursor;
        }
        while (cursor is not null);
        count.Should().Be(100);
    }
}
