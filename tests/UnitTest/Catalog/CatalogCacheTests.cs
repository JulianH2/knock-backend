using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Data;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Catalog;

/// <summary>Spec 010, R-01 y R-04: claves fijas por dato (aislamiento) y vigencia inicial de 24 horas para los catálogos.</summary>
public sealed class CatalogCacheTests
{
    [Fact]
    public void CatalogEntriesLiveAbout24Hours_CA_010_01()
    {
        new CatalogCacheOptions().Lifetime.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void OficiosUseASingleSharedKey_CA_010_01()
    {
        RedisCatalogCache.OficiosKey.Should().Be("catalog:oficios");
    }

    [Fact]
    public void NeighborhoodNamesKeysVaryByPostalCode_CA_010_01()
    {
        RedisCatalogCache.NeighborhoodNamesKey("42803").Should().Be("catalog:colonias:cp:42803");
        RedisCatalogCache.NeighborhoodNamesKey("42800").Should().Be("catalog:colonias:cp:42800");
        RedisCatalogCache.NeighborhoodNamesKey("42803").Should().NotBe(RedisCatalogCache.NeighborhoodNamesKey("42800"));
    }

    [Fact]
    public void ColoniaExistsKeysVaryByIdAndPostalCode_CA_010_01()
    {
        var id = ObjectId.Parse("0123456789abcdef01234567");

        RedisCatalogCache.ColoniaExistsKey(id, "42800").Should().Be("catalog:colonias:existe:0123456789abcdef01234567:42800");
        RedisCatalogCache.ColoniaExistsKey(id, "42803").Should().NotBe(RedisCatalogCache.ColoniaExistsKey(id, "42800"));
        RedisCatalogCache.ColoniaExistsKey(id, "42803").Should().NotBe(RedisCatalogCache.NeighborhoodNamesKey("42803"));
    }

    [Fact]
    public void KeyFamiliesDoNotOverlap_CA_010_01()
    {
        RedisCatalogCache.OficiosKey.Should().NotStartWith(RedisCatalogCache.NeighborhoodNamesKeyPrefix);
        RedisCatalogCache.NeighborhoodNamesKeyPrefix.Should().NotBe(RedisCatalogCache.ColoniaExistsKeyPrefix);
    }
}
