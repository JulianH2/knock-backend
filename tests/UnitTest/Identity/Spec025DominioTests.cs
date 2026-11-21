using AwesomeAssertions;
using Knock.Identity.Api.Modules.Trust.Domain;
using Knock.Identity.Api.Modules.Trust.Dtos;
using Knock.Identity.Api.Modules.Trust.Validators;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 025, T-02: niveles, tipos de foto, formato de la CURP y catálogo de motivos. CURP ficticias.</summary>
public sealed class Spec025DominioTests
{
    private const string _validCurp = "GODE561231HDFRRN09";

    [Theory]
    [InlineData("GODE561231HDFRRN0")]
    [InlineData("GODE561231XDFRRN09")]
    [InlineData("GODE561331HDFRRN09")]
    [InlineData("GODE561231HZZRRN09")]
    [InlineData("1234567890ABCDEFGH")]
    [InlineData("")]
    [InlineData(null)]
    public void Curp_Mal_Formada_No_Es_Valida_CA_025_05(string? curp) => Curp.IsValid(curp).Should().BeFalse();

    [Theory]
    [InlineData(_validCurp)]
    [InlineData("  gode561231hdfrrn09  ")]
    public void Curp_Bien_Formada_Es_Valida_Sin_Importar_Mayusculas_Ni_Espacios_CA_025_05(string curp) => Curp.IsValid(curp).Should().BeTrue();

    [Fact]
    public void Nivel_Desconocido_Responde_422_En_Nivel_CA_025_04()
    {
        var result = new CreateVerificationRequestValidator().Validate(new CreateVerificationRequest("premium", true, "1.0", _validCurp));
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Nivel").Which.ErrorMessage.Should().NotContain("premium");
    }

    [Theory]
    [InlineData("basica")]
    [InlineData("completa")]
    public void Nivel_Conocido_Es_Valido_CA_025_04(string level) =>
        new CreateVerificationRequestValidator().Validate(new CreateVerificationRequest(level, true, "1.0", _validCurp)).IsValid.Should().BeTrue();

    [Fact]
    public void Fotos_Por_Nivel_CA_025_03() =>
        VerificationLevels.PhotosFor("basica").Should().Equal("ine_frente", "selfie");

    [Fact]
    public void Nivel_Completo_Pide_Tres_Fotos_CA_025_04() =>
        VerificationLevels.PhotosFor("completa").Should().Equal("ine_frente", "ine_reverso", "selfie");

    [Theory]
    [InlineData("foto_ilegible")]
    [InlineData("datos_no_coinciden")]
    [InlineData("selfie_no_coincide")]
    [InlineData("documento_no_valido")]
    [InlineData("fotos_borradas")]
    public void Cada_Motivo_Trae_De_Uno_A_Tres_Consejos_CA_025_15(string reason) =>
        VerificationReasons.TipsFor(reason).Count.Should().BeInRange(1, 3);

    [Fact]
    public void Motivo_Desconocido_No_Trae_Consejos_CA_025_15() => VerificationReasons.TipsFor("otro").Should().BeEmpty();
}
