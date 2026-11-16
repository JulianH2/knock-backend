using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Controllers.V1;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Xunit;

namespace Knock.UnitTest.Catalog;

/// <summary>Spec 010, R-03 y R-06: ETag débil determinista a partir del contenido y comparación con If-None-Match (304 sin cuerpo).</summary>
public sealed class CatalogETagTests
{
    private static readonly IReadOnlyList<OficioResponse> _sample =
        [new OficioResponse("ele", "Electricidad", "domicilio"), new OficioResponse("plo", "Plomería", "domicilio")];

    [Fact]
    public void ETag_IsWeakAndDeterministicForTheSameContent_CA_010_03()
    {
        var first = OficiosController.ETag(_sample);
        var second = OficiosController.ETag(_sample);

        first.Should().StartWith("W/\"");
        second.Should().Be(first);
    }

    [Fact]
    public void ETag_ChangesWhenAnyFieldChanges_CA_010_03()
    {
        var etag = OficiosController.ETag(_sample);
        var changed = OficiosController.ETag([new OficioResponse("ele", "Electricidad", "taller"), _sample[1]]);
        var added = OficiosController.ETag([.. _sample, new OficioResponse("mec", "Mecánica", "taller")]);

        changed.Should().NotBe(etag);
        added.Should().NotBe(etag);
    }

    [Fact]
    public void ETag_DiffersBetweenDifferentCatalogs_CA_010_03()
    {
        var other = OficiosController.ETag([new OficioResponse("ele", "Electricidad", "domicilio")]);

        other.Should().NotBe(OficiosController.ETag(_sample));
    }

    [Fact]
    public void Matches_ReturnsTrueForExactWeakTag_CA_010_03()
    {
        var etag = OficiosController.ETag(_sample);

        OficiosController.Matches(etag, etag).Should().BeTrue();
    }

    [Fact]
    public void Matches_ReturnsTrueForStrongFormOfSameTag_CA_010_03()
    {
        var etag = OficiosController.ETag(_sample);
        var strong = etag[2..];

        OficiosController.Matches(strong, etag).Should().BeTrue();
    }

    [Fact]
    public void Matches_ReturnsTrueForListAndStar_CA_010_03()
    {
        var etag = OficiosController.ETag(_sample);

        OficiosController.Matches($"W/\"other\", {etag}", etag).Should().BeTrue();
        OficiosController.Matches("*", etag).Should().BeTrue();
    }

    [Fact]
    public void Matches_ReturnsFalseForDifferentOrEmpty_CA_010_03()
    {
        var etag = OficiosController.ETag(_sample);

        OficiosController.Matches("W/\"other\"", etag).Should().BeFalse();
        OficiosController.Matches("", etag).Should().BeFalse();
        OficiosController.Matches("   ", etag).Should().BeFalse();
    }
}
