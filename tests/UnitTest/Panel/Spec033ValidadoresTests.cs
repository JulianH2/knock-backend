using AwesomeAssertions;
using FluentValidation.Results;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Knock.Identity.Api.Modules.Panel.Validators;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-01: forma de los cuerpos de sesión y activación del panel (R-04, R-08 y R-29). Un cuerpo inválido es 422; el ProblemDetails lo arma el filtro común.</summary>
public class Spec033ValidadoresTests
{
    private const string _challenge = "AAAAAAAAAAAAAAAAAAAAAA";
    private static readonly string _activationToken = new('a', 43);

    private static ValidationResult Celular(string? idToken) => new PanelCelularRequestValidator().Validate(new PanelCelularRequest(idToken));

    private static ValidationResult Totp(string? challenge, string? code) => new PanelTotpRequestValidator().Validate(new PanelTotpRequest(challenge, code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Celular_SinIdToken_Falla_En_Su_Campo_CA_033_02(string? idToken) =>
        Celular(idToken).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("IdToken");

    [Fact]
    public void Celular_IdToken_De_4096_Pasa_Y_De_4097_Falla_R_04()
    {
        Celular(new string('x', 4096)).IsValid.Should().BeTrue();
        Celular(new string('x', 4097)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    [InlineData("12 456")]
    [InlineData("١٢٣٤٥٦")]
    public void Totp_Codigo_Que_Falta_O_No_Tiene_Seis_Digitos_Es_422_CA_033_02(string? code) =>
        Totp(_challenge, code).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Codigo");

    [Fact]
    public void Totp_Codigo_De_Seis_Digitos_Con_Ceros_A_La_Izquierda_Pasa_R_08() =>
        Totp(_challenge, "000123").IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("corto")]
    [InlineData("AAAAAAAAAAAAAAAAAAAA+/")]
    public void Totp_Desafio_Que_Falta_O_No_Es_Base64Url_De_128_Bits_Es_422_R_08(string? challenge) =>
        Totp(challenge, "123456").Errors.Should().ContainSingle().Which.PropertyName.Should().Be("DesafioId");

    [Fact]
    public void Validador_No_Repite_El_Valor_Enviado_En_Los_Mensajes_R_04()
    {
        var result = Totp(_challenge, "12ab56");

        result.Errors.Should().OnlyContain(error => !error.ErrorMessage.Contains("12ab56", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("corto", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    public void Activacion_Token_De_256_Bits_En_Base64Url_R_29(string? token, bool valid) =>
        new PanelActivacionRequestValidator().Validate(new PanelActivacionRequest(token)).IsValid.Should().Be(valid);

    [Fact]
    public void Activacion_Token_Con_Caracteres_Fuera_De_Base64Url_Falla_R_29() =>
        new PanelActivacionRequestValidator().Validate(new PanelActivacionRequest(_activationToken[..42] + "+")).IsValid.Should().BeFalse();

    [Fact]
    public void Los_Dtos_Con_Secretos_No_Se_Imprimen_R_28()
    {
        new PanelCelularRequest("token-secreto").ToString().Should().NotContain("secreto");
        new PanelTotpRequest(_challenge, "123456").ToString().Should().NotContain("123456").And.NotContain(_challenge);
        new PanelAccesoResponse("jwt-secreto", DateTime.UtcNow, [], "Ana").ToString().Should().NotContain("secreto");
        new PanelActivacionResponse("otpauth-secreto", "<svg/>").ToString().Should().NotContain("secreto");
    }
}
