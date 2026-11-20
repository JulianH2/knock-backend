using System.Diagnostics;
using System.Reflection;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Assistant.Data;
using Knock.Market.Api.Modules.Assistant.Services;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Assistant;

/// <summary>
/// Spec 024, R-02 y R-03 (CA-01, CA-02 y CA-13 en la parte del filtro): ningún dato personal sale del filtro. Todos los datos son sintéticos: ningún valor pertenece a una persona
/// real (CURP y claves con la forma correcta pero inventadas, teléfonos 55 5555 xxxx, la tarjeta de pruebas pública 4111…, dominio <c>ejemplo.com</c>).
/// </summary>
public sealed class Spec024FiltroTests
{
    private const string _curp = "ABCD900101HDFXYZ01";
    private const string _electorKey = "ABCDEF90010109H123";
    private const string _rfc = "ABCD900101AB1";

    [Fact]
    public void Censura_Datos_Personales_CA_024_01()
    {
        var text = $"Hola, soy yo, mi teléfono es 55 5555 1234, mi CURP {_curp}, escríbeme a persona.prueba@ejemplo.com y vivo en Calle Hidalgo 123, Col. Centro. Se me rompió el boiler.";

        var result = FiltroDatosPersonales.Censurar(text);

        result.Text.Should().NotContainAny("5555 1234", _curp, "persona.prueba@ejemplo.com", "Hidalgo", "Centro");
        result.Text.Should().Contain("Se me rompió el boiler.").And.Contain("[teléfono]").And.Contain("[curp]").And.Contain("[correo]").And.Contain("[dirección]");
        result.Types.Should().BeEquivalentTo(
        [
            new CensorshipEntry("curp", 1),
            new CensorshipEntry("email", 1),
            new CensorshipEntry("phone", 1),
            new CensorshipEntry("address", 1),
        ]);
    }

    [Theory]
    [InlineData("mi clave ABCD900101QQQQQQ12 va aquí")] // parece CURP (4 letras + 6 dígitos + 8) pero no cumple el patrón completo
    [InlineData("mi clave ABCD900101hdfxyz0X va aquí")] // último carácter no es dígito
    [InlineData("mi clave 12AB34CD56EF78GH90 va aquí")] // 18 caracteres mezclados: documento de identidad dudoso
    [InlineData("mi clave ab12cd34ef56gh78ij va aquí")] // en minúsculas
    public void Falla_Cerrado_CA_024_02(string text)
    {
        var result = FiltroDatosPersonales.Censurar(text);

        result.Text.Should().Be("mi clave " + (result.Types[0].Type == "curp" ? "[curp]" : "[identificación]") + " va aquí");
        result.Types.Should().ContainSingle().Which.Count.Should().Be(1);
    }

    [Theory]
    [InlineData("llámame al 5555551234", "llámame al [teléfono]")]
    [InlineData("55 5555 1234", "[teléfono]")]
    [InlineData("55-5555-1234", "[teléfono]")]
    [InlineData("555-555-1234", "[teléfono]")]
    [InlineData("555.555.1234", "[teléfono]")]
    [InlineData("(555) 555 1234", "[teléfono]")]
    [InlineData("(555) 555-1234 gracias", "[teléfono] gracias")]
    [InlineData("55 55 55 12 34", "[teléfono]")]
    [InlineData("5 5 5 5 5 5 1 2 3 4", "[teléfono]")]
    [InlineData("+52 55 5555 1234", "[teléfono]")]
    [InlineData("+52 1 55 5555 1234", "[teléfono]")]
    [InlineData("+525555551234", "[teléfono]")]
    [InlineData("0445555551234", "[teléfono]")]
    [InlineData("52 5555551234", "[teléfono]")]
    [InlineData("whatsapp5555551234ok", "whatsapp[teléfono]ok")]
    [InlineData("el número es 555\n555\n1234", "el número es [teléfono]")]
    [InlineData("Tel: 771 555 1234.", "Tel: [teléfono].")]
    public void Censura_Telefonos_En_Varios_Formatos_CA_024_01(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("persona@ejemplo.com", "[correo]")]
    [InlineData("PERSONA.Prueba+knock@sub.ejemplo.com.mx", "[correo]")]
    [InlineData("escríbeme a persona (at) ejemplo.com ya", "escríbeme a [correo] ya")]
    [InlineData("persona [at] ejemplo punto com", "[correo]")]
    [InlineData("persona arroba ejemplo punto com", "[correo]")]
    [InlineData("personaＡ＠ejemplo.com", "[correo]")] // arroba y letra de ancho completo
    public void Censura_Correos_CA_024_01(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("ABCD900101HDFXYZ01")]
    [InlineData("abcd900101hdfxyz01")]
    [InlineData("AbCd900101HdFxYz01")]
    [InlineData("ABCD 900101 H DF XYZ 01")]
    [InlineData("ABCD-900101-HDF-XYZ-01")]
    [InlineData("A B C D 9 0 0 1 0 1 H D F X Y Z 0 1")]
    [InlineData("ABCD900101\nHDFXYZ01")]
    [InlineData("ＡＢＣＤ９００１０１ＨＤＦＸＹＺ０１")] // ancho completo
    [InlineData("ABCD٩٠٠١٠١HDFXYZ٠١")] // dígitos arábigo-índicos
    [InlineData("ABCD𝟗𝟎𝟎𝟏𝟎𝟏HDFXYZ01")] // dígitos matemáticos (pares sustitutos)
    [InlineData("АВСD900101HDFXYZ01")] // А, В y С cirílicas
    [InlineData("ΑΒCD900101HDFXYZ01")] // Α y Β griegas
    [InlineData("ABCD90​0101HD‍FXYZ01")] // espacios de ancho cero
    [InlineData("ABCD900101­HDFXYZ01")] // guion suave
    [InlineData("ABCD900101HDFXYŹ​01")] // marca combinante
    [InlineData("ABCD‑900101–HDF—XYZ−01")] // guiones Unicode
    public void Censura_Curp_Con_Entradas_Adversarias_CA_024_02(string curp)
    {
        var result = FiltroDatosPersonales.Censurar("mi curp: " + curp + ", gracias");

        result.Text.Should().Be("mi curp: [curp], gracias");
        result.Types.Should().ContainSingle().Which.Should().Be(new CensorshipEntry("curp", 1));
    }

    [Theory]
    [InlineData("ABCDEF90010109H123")]
    [InlineData("abcdef90010109h123")]
    [InlineData("ABCDEF 90010109 H 123")]
    [InlineData("ABCDEF-90010109-H-123")]
    public void Censura_Clave_De_Elector_CA_024_02(string key)
    {
        var result = FiltroDatosPersonales.Censurar("clave " + key + " fin");

        result.Text.Should().Be("clave [identificación] fin");
        result.Types.Should().ContainSingle().Which.Should().Be(new CensorshipEntry("identity_document", 1));
    }

    [Theory]
    [InlineData("mi CIC es 123456789", "mi [identificación]")]
    [InlineData("CIC: 123 456 789", "[identificación]")]
    [InlineData("OCR 1234567890123", "[identificación]")]
    [InlineData("ocr: 1234 5678 9012 3", "[identificación]")]
    [InlineData("mi número es 1234567890123 ok", "mi número es [identificación] ok")]
    [InlineData("IDMEX1234567890<<1234567890123", "[identificación]")]
    [InlineData("Clave de elector 123456789012", "[identificación]")]
    public void Censura_Ocr_Y_Cic_De_La_Ine_CA_024_02(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("mi rfc es ABCD900101AB1 gracias", "mi rfc es [rfc] gracias")]
    [InlineData("rfc abcd900101ab1", "rfc [rfc]")]
    [InlineData("persona moral ABC900101AB1 sa", "persona moral [rfc] sa")]
    [InlineData("ABCD 900101 AB1", "[rfc]")]
    [InlineData("AB&C900101AB1", "[rfc]")]
    [InlineData("ÑBCD900101AB1", "[rfc]")]
    public void Censura_Rfc_CA_024_01(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("4111111111111111", "[tarjeta]")]
    [InlineData("4111 1111 1111 1111", "[tarjeta]")]
    [InlineData("4111-1111-1111-1111", "[tarjeta]")]
    [InlineData("tarjeta 4111 1111 1111 1111 vence 12/30", "tarjeta [tarjeta] vence 12/30")]
    [InlineData("3400 000000 00009", "[tarjeta]")]
    public void Censura_Tarjetas_CA_024_01(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("vivo en Calle Hidalgo 123", "[dirección]")]
    [InlineData("calle hidalgo #123", "[dirección]")]
    [InlineData("Calle Hidalgo No. 123, Col. Centro", "[dirección]")]
    [InlineData("CALLE HIDALGO NUM 123 COLONIA LAS FLORES", "[dirección]")]
    [InlineData("Av. Juárez 45 interior 3, Colonia Centro, C.P. 62000", "[dirección]")]
    [InlineData("Avenida 20 de Noviembre 45", "[dirección]")]
    [InlineData("Blvd. Benito Juárez 1500", "[dirección]")]
    [InlineData("Privada de los Pinos 12A", "[dirección]")]
    [InlineData("Cerrada Hidalgo s/n, Fracc. Los Pinos", "[dirección]")]
    [InlineData("Calz. de Tlalpan 3000 piso 2", "[dirección]")]
    [InlineData("Carretera Federal 95 km 14", "[dirección] km 14")]
    [InlineData("C. Morelos 8, Barrio Alto", "[dirección]")]
    [InlineData("mi domicilio: Insurgentes 1234", "mi [dirección]")]
    [InlineData("Dirección Hidalgo 12 Centro", "[dirección] Centro")]
    [InlineData("CALLE​ HIDALGO\n123", "[dirección]")]
    [InlineData("ＣＡＬＬＥ Hidalgo １２３", "[dirección]")]
    [InlineData("Сalle Hidalgo 123", "[dirección]")] // «С» cirílica
    public void Censura_Direcciones_CA_024_01(string text, string expected)
    {
        FiltroDatosPersonales.Censurar(text).Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("Se me rompió la llave del baño y gotea desde hace 3 días.")]
    [InlineData("El departamento tiene 2 baños y 3 recámaras.")]
    [InlineData("La calle está inundada desde ayer.")]
    [InlineData("En mi calle hay 3 coladeras tapadas y la avenida también.")]
    [InlineData("La calle Hidalgo tiene 2 coladeras rotas.")]
    [InlineData("Necesito un plomero hoy a las 10:30 por 1,500 pesos.")]
    [InlineData("Presupuesto de $12,500.00 pesos más IVA.")]
    [InlineData("Reservé el 2026-11-19 a las 10 horas.")]
    [InlineData("Fecha 19-11-2026 10 personas")]
    [InlineData("Compré 12 metros de cable calibre 14 y 3 interruptores.")]
    [InlineData("Folio 20261119 y pedido 123456")]
    [InlineData("El número 1234567 y el código 98765432 son solo referencias.")]
    [InlineData("Número de 11 dígitos 55555512345 sin lada.")]
    [InlineData("Medidas 120 x 80 x 45 cm, peso 35 kg, potencia 1500 W.")]
    [InlineData("Llámame mañana, el fregadero se tapó otra vez y no baja el agua.")]
    [InlineData("Tengo 2do piso, 5 ventanas y 1 puerta con 3 chapas.")]
    [InlineData("Necesito instalar un minisplit de 12000 BTU de la marca X en la recámara.")]
    [InlineData("Mi casa está en un tercer piso sin elevador")]
    [InlineData("Paseo con mi perro 2 horas")]
    [InlineData("")]
    [InlineData("   ")]
    public void No_Toca_Texto_Normal_CA_024_01(string text)
    {
        var result = FiltroDatosPersonales.Censurar(text);

        result.Text.Should().Be(text);
        result.Types.Should().BeEmpty();
    }

    [Fact]
    public void Cuenta_Cada_Hallazgo_Y_No_Mezcla_Tipos_CA_024_01()
    {
        var result = FiltroDatosPersonales.Censurar("a 5555551234 b 5555559876 c a@ejemplo.com d b@ejemplo.com e c@ejemplo.com");

        result.Text.Should().Be("a [teléfono] b [teléfono] c [correo] d [correo] e [correo]");
        result.Types.Should().BeEquivalentTo([new CensorshipEntry("email", 3), new CensorshipEntry("phone", 2)]);
    }

    [Fact]
    public void Traslape_De_Tipos_Se_Censura_Una_Sola_Vez_CA_024_02()
    {
        var result = FiltroDatosPersonales.Censurar($"curp {_curp} y clave {_electorKey}");

        result.Text.Should().Be("curp [curp] y clave [identificación]");
        result.Types.Should().HaveCount(2);
    }

    [Fact]
    public void Es_Idempotente_Y_No_Deja_Marcadores_Que_Se_Vuelvan_A_Censurar_CA_024_01()
    {
        var first = FiltroDatosPersonales.Censurar($"{_curp} 5555551234 persona@ejemplo.com Calle Hidalgo 123 {_rfc} 4111 1111 1111 1111");

        var second = FiltroDatosPersonales.Censurar(first.Text);

        second.Text.Should().Be(first.Text);
        second.Types.Should().BeEmpty();
    }

    [Fact]
    public void Texto_Mixto_Conserva_Lo_Que_No_Es_Dato_Con_Acentos_Y_Emoji_CA_024_01()
    {
        var result = FiltroDatosPersonales.Censurar("Ñandú 😀 ¿Quién arregla la fuga? Mi teléfono: 5555551234 😀 ¡Gracias!");

        result.Text.Should().Be("Ñandú 😀 ¿Quién arregla la fuga? Mi teléfono: [teléfono] 😀 ¡Gracias!");
    }

    [Fact]
    public void Texto_Partido_Se_Censura_Al_Filtrar_El_Texto_Completo_CA_024_02()
    {
        // El filtro no guarda estado entre llamadas: quien arma el texto (varios mensajes) debe unirlo y filtrar la unión.
        const string first = "mi curp empieza con ABCD9001";
        const string second = "01HDFXYZ01 termina aquí";

        FiltroDatosPersonales.Censurar(first).Text.Should().Be(first);
        FiltroDatosPersonales.Censurar(second).Text.Should().Be(second);
        FiltroDatosPersonales.Censurar(first + second).Text.Should().Be("mi curp empieza con [curp] termina aquí");
        FiltroDatosPersonales.Censurar(first + '\n' + second).Text.Should().Be("mi curp empieza con [curp] termina aquí");
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(4001)]
    public void Texto_En_El_Limite_De_Entrada_Se_Filtra_Completo_CA_024_01(int length)
    {
        var filler = new string('a', length - 24 - 1);
        var text = filler + " " + "5555551234" + " " + "xxxx" + " " + "a@ejemplo.com";
        text = text[..Math.Min(text.Length, length)];

        var result = FiltroDatosPersonales.Censurar(text);

        result.Text.Should().NotContain("5555551234");
        result.Text.Length.Should().BeLessThanOrEqualTo(length);
    }

    [Fact]
    public void Filtrar_Antes_De_Recortar_Evita_Datos_Partidos_CA_024_02()
    {
        var text = new string('a', 3995) + " 5555551234";

        var filtered = FiltroDatosPersonales.Censurar(text).Text;
        var cut = filtered[..Math.Min(filtered.Length, 4000)];

        cut.Should().NotMatchRegex(@"\d{4}");
    }

    [Fact]
    public void Sin_Valores_En_El_Resultado_Ni_En_Su_Representacion_Ni_En_El_Registro_De_Uso_CA_024_13()
    {
        var secrets = new[] { _curp, _electorKey, "5555551234", "persona@ejemplo.com", "Calle Hidalgo 123", "4111111111111111" };
        var result = FiltroDatosPersonales.Censurar(string.Join(' ', secrets));

        var typesDump = string.Join(',', result.Types.Select(t => $"{t.Type}:{t.Count}"));
        foreach (var secret in secrets)
        {
            typesDump.Should().NotContain(secret);
            result.ToString().Should().NotContain(secret);
        }

        typeof(FilterResult).GetProperties().Select(p => p.Name).Should().BeEquivalentTo("Text", "Types");
        typeof(CensorshipEntry).GetProperties().Select(p => p.Name).Should().BeEquivalentTo("Type", "Count");
        typeof(UsageRecord).GetProperties().Select(p => p.Name).Should().BeEquivalentTo(
            "UserId", "Function", "Model", "PromptVersion", "InputTokens", "OutputTokens", "LatencyMs", "Origin", "FallbackReason", "Truncated", "CreatedAt");
    }

    [Fact]
    public void El_Filtro_No_Tiene_Dependencia_De_Registro_Ni_De_Consola_CA_024_13()
    {
        var result = Types.InAssembly(typeof(FiltroDatosPersonales).Assembly)
            .That().HaveName("FiltroDatosPersonales")
            .Should().NotHaveDependencyOnAny("Microsoft.Extensions.Logging", "System.Console", "Serilog", "OpenTelemetry", "System.Diagnostics")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypes ?? []));
        typeof(FiltroDatosPersonales).GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).OfType<FieldInfo>()
            .Where(f => !f.IsLiteral).Should().OnlyContain(f => f.IsInitOnly, "sin estado mutable");
    }

    [Fact]
    public void Nulo_Falla_Sin_Revelar_Texto_CA_024_13()
    {
        var act = () => FiltroDatosPersonales.Censurar(null!);

        act.Should().Throw<ArgumentNullException>().Which.Message.Should().NotContain(_curp);
    }

    [Theory]
    [InlineData("1 ")]
    [InlineData("a-")]
    [InlineData("calle ")]
    [InlineData("0000000000000000000000000000000000000000x")]
    public void No_Se_Congela_Con_Entradas_Patologicas_CA_024_01(string unit)
    {
        var text = string.Concat(Enumerable.Repeat(unit, 4000 / unit.Length));
        var watch = Stopwatch.StartNew();

        var result = FiltroDatosPersonales.Censurar(text);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        result.Text.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void Filtra_4_KB_En_Menos_De_5_Ms_CA_024_01()
    {
        var text = string.Concat(Enumerable.Repeat("Se me rompió el boiler, vivo en Calle Hidalgo 123 y mi teléfono es 5555551234. ", 60))[..4000];
        var warmUp = Stopwatch.StartNew();
        while (warmUp.ElapsedMilliseconds < 500)
        {
            FiltroDatosPersonales.Censurar(text);
        }

        // Se toma el mejor de varios lotes: mide el costo del filtro y no el ruido de otras pruebas que corren en paralelo.
        const int runs = 20;
        var best = double.MaxValue;
        for (var batch = 0; batch < 7; batch++)
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < runs; i++)
            {
                FiltroDatosPersonales.Censurar(text);
            }

            best = Math.Min(best, watch.Elapsed.TotalMilliseconds / runs);
        }

        best.Should().BeLessThan(5);
    }
}
