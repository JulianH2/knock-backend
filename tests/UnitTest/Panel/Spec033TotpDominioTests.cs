using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.Identity.Api.Modules.Panel.Services;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-02: TOTP de RFC 6238 con reloj simulado (ventana, paso mayor) y cifrado del secreto (R-09, R-26).</summary>
public class Spec033TotpDominioTests
{
    private static readonly byte[] _rfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");
    private static readonly DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_015);

    private static long Step => TotpService.StepOf(_now);

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static TotpService Service()
    {
        var options = new PanelTotpOptions { KeyVersion = 1 };
        options.Keys[1] = NewKey();
        return new TotpService(PanelTotpKeys.TryCreate(options, out _)!);
    }

    [Theory]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void Codigos_De_Los_Vectores_De_RFC_6238_Con_SHA1_Y_6_Digitos_R_09(long unixSeconds, string expected) =>
        TotpService.CodeFor(_rfcSecret, unixSeconds / TotpService.StepSeconds).Should().Be(expected);

    [Fact]
    public void Acepta_El_Paso_Anterior_El_Actual_Y_El_Siguiente_R_09()
    {
        foreach (var step in new[] { Step - 1, Step, Step + 1 })
        {
            TotpService.Verify(_rfcSecret, TotpService.CodeFor(_rfcSecret, step), _now, 0).Should().Be(step);
        }
    }

    [Fact]
    public void Rechaza_Un_Codigo_De_Dos_Pasos_Atras_O_Adelante_CA_033_02()
    {
        TotpService.Verify(_rfcSecret, TotpService.CodeFor(_rfcSecret, Step - 2), _now, 0).Should().BeNull();
        TotpService.Verify(_rfcSecret, TotpService.CodeFor(_rfcSecret, Step + 2), _now, 0).Should().BeNull();
    }

    [Fact]
    public void Rechaza_Un_Codigo_Incorrecto_O_Mal_Formado_CA_033_02()
    {
        var good = TotpService.CodeFor(_rfcSecret, Step);
        var wrong = good == "000000" ? "000001" : "000000";

        TotpService.Verify(_rfcSecret, wrong, _now, 0).Should().BeNull();
        TotpService.Verify(_rfcSecret, null, _now, 0).Should().BeNull();
        TotpService.Verify(_rfcSecret, good[..5], _now, 0).Should().BeNull();
        TotpService.Verify(_rfcSecret, good + "0", _now, 0).Should().BeNull();
        TotpService.Verify(_rfcSecret, "12345a", _now, 0).Should().BeNull();
    }

    [Fact]
    public void Un_Paso_Ya_Aceptado_No_Se_Vuelve_A_Aceptar_CA_033_06()
    {
        var code = TotpService.CodeFor(_rfcSecret, Step);

        TotpService.Verify(_rfcSecret, code, _now, Step).Should().BeNull("el mismo código con un desafío nuevo dentro de su ventana es repetición");
        TotpService.Verify(_rfcSecret, code, _now, Step + 1).Should().BeNull("un paso menor al último aceptado tampoco");
    }

    [Fact]
    public void Un_Paso_Mayor_Al_Ultimo_Aceptado_Se_Acepta_CA_033_06() =>
        TotpService.Verify(_rfcSecret, TotpService.CodeFor(_rfcSecret, Step + 1), _now, Step).Should().Be(Step + 1);

    [Fact]
    public void El_Reloj_Desfasado_Hasta_30_Segundos_Se_Tolera_R_09()
    {
        var code = TotpService.CodeFor(_rfcSecret, Step);

        TotpService.Verify(_rfcSecret, code, _now.AddSeconds(29), 0).Should().NotBeNull();
        TotpService.Verify(_rfcSecret, code, _now.AddSeconds(-29), 0).Should().NotBeNull();
    }

    [Fact]
    public void El_Secreto_Generado_Es_De_160_Bits_Y_Distinto_Cada_Vez_R_29()
    {
        var first = TotpService.GenerateSecret();

        first.Should().HaveCount(20);
        TotpService.GenerateSecret().Should().NotEqual(first);
    }

    [Fact]
    public void El_Secreto_Cifrado_Se_Abre_Con_La_Misma_Cuenta_Y_Se_Cifra_Distinto_Cada_Vez_R_26()
    {
        var service = Service();
        var secret = TotpService.GenerateSecret();

        var first = service.Protect(secret, "sub-1");
        var second = service.Protect(secret, "sub-1");

        service.Unprotect(first.Ciphertext, first.KeyVersion, "sub-1").Should().Equal(secret);
        first.Ciphertext.Should().NotEqual(second.Ciphertext, "el nonce es aleatorio");
        first.Ciphertext.AsSpan().IndexOf(secret).Should().Be(-1, "el secreto no aparece en claro");
    }

    [Fact]
    public void El_Secreto_Cifrado_No_Se_Abre_En_Otra_Cuenta_Ni_Si_Se_Altera_R_26()
    {
        var service = Service();
        var protectedSecret = service.Protect(TotpService.GenerateSecret(), "sub-1");
        var tampered = (byte[])protectedSecret.Ciphertext.Clone();
        tampered[14] ^= 1;

        FluentActions.Invoking(() => service.Unprotect(protectedSecret.Ciphertext, protectedSecret.KeyVersion, "sub-2")).Should().Throw<CryptographicException>();
        FluentActions.Invoking(() => service.Unprotect(tampered, protectedSecret.KeyVersion, "sub-1")).Should().Throw<CryptographicException>();
        FluentActions.Invoking(() => service.Unprotect(protectedSecret.Ciphertext, 99, "sub-1")).Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Una_Llave_Nueva_Cifra_Y_Las_Cuentas_Viejas_Siguen_Abriendo_Con_Su_Version_R_26()
    {
        var options = new PanelTotpOptions { KeyVersion = 1 };
        options.Keys[1] = NewKey();
        var old = new TotpService(PanelTotpKeys.TryCreate(options, out _)!);
        var secret = TotpService.GenerateSecret();
        var stored = old.Protect(secret, "sub-1");
        options.Keys[2] = NewKey();
        options.KeyVersion = 2;
        var rotated = new TotpService(PanelTotpKeys.TryCreate(options, out _)!);

        rotated.Unprotect(stored.Ciphertext, stored.KeyVersion, "sub-1").Should().Equal(secret);
        rotated.Protect(secret, "sub-1").KeyVersion.Should().Be(2);
    }

    [Theory]
    [InlineData("no-es-base64")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    public void Las_Llaves_Invalidas_No_Se_Aceptan_Y_El_Mensaje_No_Trae_El_Valor_R_26(string key)
    {
        var options = new PanelTotpOptions { KeyVersion = 1 };
        options.Keys[1] = key;

        var keys = PanelTotpKeys.TryCreate(options, out var errors);

        keys.Should().BeNull();
        errors.Should().NotBeEmpty().And.OnlyContain(error => !error.Contains(key, StringComparison.Ordinal));
    }

    [Fact]
    public void Sin_Llaves_Configuradas_No_Hay_Servicio_R_26() =>
        PanelTotpKeys.TryCreate(new PanelTotpOptions(), out _).Should().BeNull();

    [Fact]
    public void La_Direccion_Otpauth_Lleva_El_Secreto_En_Base32_Y_Los_Parametros_Del_Estandar_R_09()
    {
        var uri = TotpService.BuildOtpauthUri(_rfcSecret, "Ana");

        uri.Should().StartWith("otpauth://totp/").And.Contain("secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ").And.Contain("digits=6").And.Contain("period=30");
    }
}
