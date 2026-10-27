using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Xunit;

namespace Knock.UnitTest.Identity;

// TC_TT_API_02_10 (spec firmada de TT-API-02) queda reemplazado por TC-TT-API-12-01 a 04: los métodos de derivación conservan también
// ese sufijo para que `sdd_lint.py --trace TT-API-02` no marque drift.
public class UserIdDerivationTests
{
    // Valores esperados calculados con una implementación independiente (Python `hashlib.sha256`, RFC 9562 §5.8) con
    // NAMESPACE = bcfea58b-95dd-4c0e-80d1-baf839f2e36e. Los uid son ficticios.
    [Theory]
    [InlineData("uid-prueba-0001", "dba04c44-65fd-8390-8279-e3940c6e8663")]
    [InlineData("uid-prueba-0002", "e0743e2a-1841-8077-a348-2dcfdf0be576")]
    [InlineData("wVSEsCC4m04ROIgLCrHUGoxMyjn5", "ebaf9b54-0189-8919-ac36-385cf0c45584")]
    [InlineData("nándú-π", "e7176ed4-649c-8c0b-b884-3e853874fbe2")]
    // Frontera del relleno de bloque: 16 + 39 = 55 bytes caben en un bloque; 16 + 40 = 56 ya usan dos.
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "888a6ce1-0249-8d75-b89f-e46b59d9bd78")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "403e0242-dc63-83f3-bf68-4c80cef2b43c")]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "3a3c03c3-269d-82ae-b237-c83ad91982f9")]
    public void KnownVector_MatchesUuidV8Sha256_TC_TT_API_12_01_TC_TT_API_02_10(string uid, string expected)
    {
        var derived = UserIdDerivation.Derive(uid);

        derived.Should().Be(Guid.Parse(expected));
    }

    [Fact]
    public void SameUidIsStableDifferentUidsDifferAndNamespaceIsFixed_TC_TT_API_12_02_TC_TT_API_02_10()
    {
        var first = UserIdDerivation.Derive("uid-prueba-0001");
        var second = UserIdDerivation.Derive("uid-prueba-0002");

        UserIdDerivation.Namespace.Should().Be(Guid.Parse("bcfea58b-95dd-4c0e-80d1-baf839f2e36e"));
        first.Should().NotBe(second);
        for (var i = 0; i < 5; i++)
        {
            UserIdDerivation.Derive("uid-prueba-0001").Should().Be(first);
        }
    }

    [Theory]
    [InlineData("uid-prueba-0001")]
    [InlineData("uid-prueba-0002")]
    [InlineData("nándú-π")]
    [InlineData("a")]
    public void SetsVersion8AndRfcVariantKeepingTheSha256Bits_TC_TT_API_12_03_TC_TT_API_02_10(string uid)
    {
        var derived = UserIdDerivation.Derive(uid);
        var text = derived.ToString("D");

        text[14].Should().Be('8', "el dígito de versión es 8 (UUID v8)");
        "89ab".Should().Contain(text[19].ToString(), "la variante es la RFC 9562");

        // Cálculo propio de la prueba (no la implementación bajo prueba): primeros 16 bytes del SHA-256 de namespace ‖ uid.
        var namespaceBytes = new byte[16];
        UserIdDerivation.Namespace.TryWriteBytes(namespaceBytes, bigEndian: true, out _);
        var expected = SHA256.HashData([.. namespaceBytes, .. Encoding.UTF8.GetBytes(uid)]).AsSpan(0, 16).ToArray();
        var actual = new byte[16];
        derived.TryWriteBytes(actual, bigEndian: true, out _);

        // Los 122 bits que no son versión ni variante son los del hash.
        actual[6].Should().Be((byte)((expected[6] & 0x0F) | 0x80));
        actual[8].Should().Be((byte)((expected[8] & 0x3F) | 0x80));
        actual.Where((_, i) => i is not 6 and not 8).Should().Equal(expected.Where((_, i) => i is not 6 and not 8));
        (actual[6] & 0x0F).Should().Be(expected[6] & 0x0F);
        (actual[8] & 0x3F).Should().Be(expected[8] & 0x3F);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\t")]
    [InlineData(null)]
    public void EmptyOrBlankUidThrowsArgumentException_TC_TT_API_12_04_TC_TT_API_02_10(string? uid)
    {
        var act = () => UserIdDerivation.Derive(uid!);

        var thrown = act.Should().Throw<ArgumentException>().Which;
        thrown.ParamName.Should().Be("uid");
        if (!string.IsNullOrEmpty(uid))
        {
            thrown.Message.Should().NotContain(uid, "el mensaje no incluye el valor recibido");
        }
    }

    [Fact]
    public void AnyValidUidDoesNotThrow_TC_TT_API_12_04()
    {
        foreach (var uid in new[] { "a", "uid-prueba-0001", "nándú-π", new string('z', 128) })
        {
            var act = () => UserIdDerivation.Derive(uid);

            act.Should().NotThrow();
        }
    }

    [Fact]
    public void PublicSignatureIsKept_TC_TT_API_12_07()
    {
        var derive = typeof(UserIdDerivation).GetMethod(
            nameof(UserIdDerivation.Derive), BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
        var ns = typeof(UserIdDerivation).GetField(nameof(UserIdDerivation.Namespace), BindingFlags.Public | BindingFlags.Static);

        derive.Should().NotBeNull("Derive(string) sigue siendo público y estático");
        derive.ReturnType.Should().Be<Guid>();
        ns.Should().NotBeNull("Namespace sigue siendo un campo público y estático");
        ns.FieldType.Should().Be<Guid>();
        ns.IsInitOnly.Should().BeTrue("el Namespace no cambia nunca");
    }
}
