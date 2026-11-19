extern alias messaging;

using AwesomeAssertions;
using messaging::Knock.Messaging.Api.Modules.Conversaciones.Domain;
using Xunit;

namespace Knock.UnitTest.Conversaciones;

/// <summary>Spec 030, R-12 y CA-04: el ocultador de contacto, función pura. Solo datos ficticios.</summary>
public sealed class Spec030OcultadorDeContactoTests
{
    private const string _hidden = "[dato de contacto oculto]";

    [Theory]
    [InlineData("llámame al 771 123 4567", $"llámame al {_hidden}")]
    [InlineData("771-123-4567", _hidden)]
    [InlineData("771.123.4567", _hidden)]
    [InlineData("7711234567", _hidden)]
    [InlineData("(771) 123 4567", _hidden)]
    [InlineData("mi número es +52 771 123 4567 gracias", $"mi número es {_hidden} gracias")]
    [InlineData("+527711234567", _hidden)]
    [InlineData("+52 (771) 123-4567", _hidden)]
    [InlineData("77 11 23 45 67", _hidden)]
    [InlineData("escríbeme a correo@ejemplo.com", $"escríbeme a {_hidden}")]
    [InlineData("CORREO.Prueba+knock@ejemplo.com.mx por favor", $"{_hidden} por favor")]
    [InlineData("mi mail: a_b-c@sub.ejemplo.org.", $"mi mail: {_hidden}.")]
    [InlineData("https://wa.me/527711234567", _hidden)]
    [InlineData("wa.me/527711234567?text=hola", _hidden)]
    [InlineData("entra a https://api.whatsapp.com/send?phone=527711234567 ya", $"entra a {_hidden} ya")]
    [InlineData("https://chat.whatsapp.com/AbCdEfG123", _hidden)]
    [InlineData("https://t.me/mi_usuario", _hidden)]
    [InlineData("T.ME/mi_usuario", _hidden)]
    [InlineData("telegram.me/mi_usuario", _hidden)]
    public void Ocultar_Hides_PhonesEmailsAndMessagingLinks_CA_030_04(string text, string expected)
    {
        var result = OcultadorDeContacto.Ocultar(text);

        result.Texto.Should().Be(expected);
        result.Oculto.Should().BeTrue();
    }

    [Fact]
    public void Ocultar_HidesEveryKindInTheSameMessage_CA_030_04()
    {
        var result = OcultadorDeContacto.Ocultar("llama al 771 123 4567 o escribe a correo@ejemplo.com o a https://t.me/yo");

        result.Texto.Should().Be($"llama al {_hidden} o escribe a {_hidden} o a {_hidden}");
        result.Oculto.Should().BeTrue();
    }

    [Theory]
    [InlineData("Llego a las 10:30 con el material")]
    [InlineData("Son $1,500 por la revisión")]
    [InlineData("La fecha es 18/11/2026")]
    [InlineData("Necesito 3 focos de 60 watts y 2 apagadores")]
    [InlineData("Mi código postal es 42800")]
    [InlineData("987654321")]
    [InlineData("El departamento 12 del edificio 345")]
    [InlineData("Escribe aquí en el chat, por favor")]
    [InlineData("whatsapp me sirve más tarde")]
    [InlineData("Es una gran idea.great.me no es un enlace")]
    [InlineData("")]
    public void Ocultar_LeavesOrdinaryTextAlone_CA_030_04(string text)
    {
        var result = OcultadorDeContacto.Ocultar(text);

        result.Texto.Should().Be(text);
        result.Oculto.Should().BeFalse();
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("77112345678")]
    public void Ocultar_DoesNotCutAnElevenDigitNumberInTheMiddle_CA_030_04(string text) =>
        OcultadorDeContacto.Ocultar(text).Texto.Should().Be(text, "once dígitos seguidos no son un teléfono de diez");

    [Fact]
    public void Ocultar_NeverKeepsTheOriginalDigits_CA_030_04()
    {
        var result = OcultadorDeContacto.Ocultar("mi cel 771 123 4567 y mi correo yo@ejemplo.com");

        result.Texto.Should().NotContain("771").And.NotContain("4567").And.NotContain("yo@ejemplo");
    }

    [Fact]
    public void Ocultar_AMessageThatIsOnlyTheMarkerStaysHiddenOnce_CA_030_04()
    {
        var result = OcultadorDeContacto.Ocultar(_hidden);

        result.Texto.Should().Be(_hidden);
        result.Oculto.Should().BeFalse("no había nada nuevo que ocultar");
    }

    [Fact]
    public void Ocultar_Null_Throws() =>
        FluentActions.Invoking(() => OcultadorDeContacto.Ocultar(null!)).Should().Throw<ArgumentNullException>();
}
