using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Knock.Market.Api.Modules.Assistant.Services;

/// <summary>Tipo de dato personal censurado; el orden es la prioridad cuando dos hallazgos se traslapan.</summary>
internal enum PersonalDataKind
{
    Curp,
    Rfc,
    IdentityDocument,
    Card,
    Email,
    Phone,
    Address,
    Indeterminate,
}

/// <summary>Cuántas veces se censuró un tipo de dato personal. Solo el tipo y la cantidad, jamás el valor.</summary>
internal readonly record struct CensorshipEntry(string Type, int Count);

/// <summary>Texto ya censurado y los tipos encontrados (spec 024, R-02 y decisión 7).</summary>
internal sealed record FilterResult(string Text, IReadOnlyList<CensorshipEntry> Types);

/// <summary>
/// Filtro de datos personales previo al envío al modelo (spec 024, R-02 y R-03). Función pura, sin dependencias, sin registros y sin estado: censura teléfonos, CURP, RFC, correos, tarjetas,
/// documentos de identidad (clave de elector, OCR y CIC de la INE) y direcciones «calle + número [+ colonia]».
/// </summary>
/// <remarks>
/// Cómo resiste a las entradas adversarias: el texto se pliega antes de buscar (dígitos y letras de ancho completo, otras escrituras de dígitos, homoglifos cirílicos y griegos, guiones
/// raros, caracteres de formato de ancho cero y marcas combinantes) y se conserva el mapa a las posiciones originales, de modo que se censura el texto original completo, no el plegado. Los
/// identificadores fuertes (CURP, clave de elector, RFC) se buscan también sin espacios ni separadores. Falla cerrado: una cadena de 18 caracteres que parece CURP o clave de elector se
/// censura aunque no cumpla el patrón completo (CA-02), y si una expresión agota su tiempo se censura el texto completo.
/// Límites conocidos (spec 024 §15): no reconoce números escritos con letras («cinco cinco…»), nombres propios ni una calle sin su palabra clave (<c>calle</c>, <c>av.</c>…); la
/// defensa en profundidad es que el cliente tampoco envía nombres ni identificadores de la cuenta. Quien lo llame debe filtrar el texto completo y solo después recortarlo (R-06), para
/// no partir un dato por la mitad. Los valores nunca se registran: esta clase no tiene registro, y las excepciones no llevan el texto.
/// </remarks>
internal static partial class FiltroDatosPersonales
{
    public const string TimeoutPlaceholder = "[texto omitido por seguridad]";

    private const int _timeoutMilliseconds = 200;
    private const RegexOptions _options = RegexOptions.CultureInvariant;

    private const string _sep = @"[\s.\-()]{0,3}";
    private const string _sepRequired = @"[\s.\-()]{1,3}";
    private const string _phonePrefix = @"(?:(?:\+|00)[\s.\-]*52[\s.\-]*(?:1[\s.\-]*)?|(?<!\d)52[\s.\-]*(?:1[\s.\-]*)?|(?<!\d)04[45][\s.\-]*)?";

    private const string _streetKeyword = @"(?:(?:CALLE|CALLEJ[OÓ]N|AVENIDA|AV|BLVD|BOULEVARD|BULEVAR|CALZADA|CALZ|CAMINO|CARRETERA|CARR|PRIVADA|PRIV|CERRADA|CDA|ANDADOR|PROLONGACI[OÓ]N|PROL|PERIF[EÉ]RICO|CIRCUITO|CTO|PASEO|RETORNO|DIAGONAL|RINCONADA)\.?|C\.)";
    private const string _notStopWord = @"(?!(?:TIENE|TIENEN|EST[AÁ]|EST[AÁ]N|HAY|DESDE|HACE|CON|QUE|SE|TENGO|NECESITO|Y|PERO|ES|SON|POR|PARA|COMO)\b)";
    private const string _streetName = @"(?:" + _notStopWord + @"[\p{L}\d'\-]+\.?)(?:\s+" + _notStopWord + @"[\p{L}\d'\-]+\.?){0,3}?";
    private const string _units = @"(?!\s*(?:D[IÍ]AS?|VECES|HORAS?|MINUTOS?|MESES|A[NÑ]OS?|SEMANAS?|PESOS|MIL|BA[NÑ]OS?|REC[AÁ]MARAS?|METROS|M2|MTS|LITROS|PIEZAS|CUARTOS|PERSONAS|VECINOS|PISOS?|%))";
    private const string _streetNumber = @"(?:(?:NO|NUM|N[UÚ]M|N[UÚ]MERO)\.?\s*|#\s*)?(?:S/N|\d{1,5}(?:-?[A-Z](?![\p{L}\d]))?(?!\d))" + _units;
    private const string _addressTail = @"(?:\s*,?\s*(?:INTERIOR|INT|DEPTO|DPTO|DEPARTAMENTO|PISO|MZ|MANZANA|LT|LOTE)\.?\s*[\p{L}\d\-]{1,6}){0,2}"
        + @"(?:\s*[,;\-]?\s*(?:COL|COLONIA|FRACC|FRACCIONAMIENTO|BARRIO|RESIDENCIAL)\.?\s+(?:" + _notStopWord + @"[\p{L}\d'\-]+(?:\s+(?=[\p{L}\d]))?){1,3})?"
        + @"(?:\s*,?\s*(?:C\.?P\.?|CP)\s*\d{5}(?!\d))?";

    public static FilterResult Censurar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        try
        {
            var plain = View.Build(texto, keepSeparators: true);
            var compact = View.Build(texto, keepSeparators: false);
            var ranges = new List<Range>();
            CollectPlain(plain, ranges);
            CollectCompact(compact, ranges);
            return Apply(texto, ranges);
        }
        catch (RegexMatchTimeoutException)
        {
            return new FilterResult(TimeoutPlaceholder, [new CensorshipEntry(Name(PersonalDataKind.Indeterminate), 1)]);
        }
    }

    /// <summary>Nombre estable (en inglés, como los motivos de respaldo) de un tipo de dato personal.</summary>
    public static string Name(PersonalDataKind kind) => kind switch
    {
        PersonalDataKind.IdentityDocument => "identity_document",
        PersonalDataKind.Curp => "curp",
        PersonalDataKind.Rfc => "rfc",
        PersonalDataKind.Card => "card",
        PersonalDataKind.Email => "email",
        PersonalDataKind.Phone => "phone",
        PersonalDataKind.Address => "address",
        _ => "indeterminate",
    };

    private static string Placeholder(PersonalDataKind kind) => kind switch
    {
        PersonalDataKind.IdentityDocument => "[identificación]",
        PersonalDataKind.Curp => "[curp]",
        PersonalDataKind.Rfc => "[rfc]",
        PersonalDataKind.Card => "[tarjeta]",
        PersonalDataKind.Email => "[correo]",
        PersonalDataKind.Phone => "[teléfono]",
        PersonalDataKind.Address => "[dirección]",
        _ => TimeoutPlaceholder,
    };

    private static void CollectPlain(View view, List<Range> ranges)
    {
        AddAll(view, IdentityLabel(), PersonalDataKind.IdentityDocument, ranges);
        AddAll(view, IdentityMachineZone(), PersonalDataKind.IdentityDocument, ranges);
        AddAll(view, OcrNumber(), PersonalDataKind.IdentityDocument, ranges);
        if (HasAlphanumericRun(view.Text, 18))
        {
            AddAll(view, LooseIdentity(), PersonalDataKind.IdentityDocument, ranges);
            AddAll(view, LooseCurp(), PersonalDataKind.Curp, ranges);
        }

        AddAll(view, RfcPlain(), PersonalDataKind.Rfc, ranges);
        AddAll(view, Card(), PersonalDataKind.Card, ranges);
        AddAll(view, Email(), PersonalDataKind.Email, ranges);
        AddAll(view, Phone(), PersonalDataKind.Phone, ranges);
        AddAll(view, StreetAddress(), PersonalDataKind.Address, ranges);
        AddAll(view, LabeledAddress(), PersonalDataKind.Address, ranges);
    }

    /// <summary>Atajo de rendimiento: las expresiones de 18 caracteres solo pueden coincidir si hay una racha alfanumérica de exactamente esa longitud.</summary>
    private static bool HasAlphanumericRun(string text, int length)
    {
        var run = 0;
        foreach (var c in text)
        {
            if (c is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            {
                run++;
            }
            else
            {
                if (run == length)
                {
                    return true;
                }

                run = 0;
            }
        }

        return run == length;
    }

    private static void CollectCompact(View view, List<Range> ranges)
    {
        AddAll(view, StrictElectorKey(), PersonalDataKind.IdentityDocument, ranges);
        AddAll(view, StrictCurp(), PersonalDataKind.Curp, ranges);
    }

    private static void AddAll(View view, Regex regex, PersonalDataKind kind, List<Range> ranges)
    {
        foreach (var match in regex.EnumerateMatches(view.Text))
        {
            if (match.Length > 0)
            {
                ranges.Add(new Range(view.Starts[match.Index], view.Ends[match.Index + match.Length - 1], kind));
            }
        }
    }

    private static FilterResult Apply(string original, List<Range> ranges)
    {
        if (ranges.Count == 0)
        {
            return new FilterResult(original, []);
        }

        var merged = Merge(ranges);
        var builder = new StringBuilder(original.Length);
        var cursor = 0;
        foreach (var range in merged)
        {
            builder.Append(original, cursor, range.Start - cursor).Append(Placeholder(range.Kind));
            cursor = range.End;
        }

        builder.Append(original, cursor, original.Length - cursor);
        var counts = merged.GroupBy(r => r.Kind).OrderBy(g => g.Key)
            .Select(g => new CensorshipEntry(Name(g.Key), g.Count())).ToList();
        return new FilterResult(builder.ToString(), counts);
    }

    private static List<Range> Merge(List<Range> ranges)
    {
        var ordered = ranges.OrderBy(r => r.Start).ThenByDescending(r => r.End).ToList();
        var merged = new List<Range>();
        foreach (var range in ordered)
        {
            if (merged.Count > 0 && range.Start < merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = new Range(last.Start, Math.Max(last.End, range.End), (PersonalDataKind)Math.Min((int)last.Kind, (int)range.Kind));
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    private readonly record struct Range(int Start, int End, PersonalDataKind Kind);

    // CURP, clave de elector y RFC: cadenas con la forma de un documento de identidad (falla cerrado).
    [GeneratedRegex(@"[A-Z]{4}\d{6}[HMX][A-Z]{2}[B-DF-HJ-NP-TV-Z]{3}[A-Z0-9]\d", _options, _timeoutMilliseconds)]
    private static partial Regex StrictCurp();

    [GeneratedRegex(@"[A-Z]{6}\d{8}[HMX]\d{3}", _options, _timeoutMilliseconds)]
    private static partial Regex StrictElectorKey();

    [GeneratedRegex(@"(?<![A-Z0-9])[A-Z]{4}\d{6}[A-Z0-9]{8}(?![A-Z0-9])", _options, _timeoutMilliseconds)]
    private static partial Regex LooseCurp();

    [GeneratedRegex(@"(?<![A-Z0-9])(?=[A-Z0-9]{18}(?![A-Z0-9]))(?=(?:[A-Z0-9]*\d){6})(?=(?:[A-Z0-9]*[A-Z]){4})[A-Z0-9]{18}", _options, _timeoutMilliseconds)]
    private static partial Regex LooseIdentity();

    [GeneratedRegex(@"(?<![A-Z0-9])[A-ZÑ&]{3,4}[ \-]?\d{2}(?:0[1-9]|1[0-2])(?:0[1-9]|[12]\d|3[01])[ \-]?[A-Z0-9]{3}(?![A-Z0-9])", _options, _timeoutMilliseconds)]
    private static partial Regex RfcPlain();

    // INE: CIC (9 dígitos) u OCR (13) con su etiqueta, línea «IDMEX…» y OCR de 13 dígitos sueltos.
    [GeneratedRegex(@"(?<![A-Z])(?:CIC|OCR|CLAVE\s+DE\s+ELECTOR)(?![A-Z])(?:\s+ES(?:\s+(?:EL|MI|LA))?)?[\s:.=#\-]{0,6}(?:\d[\s.\-]?){9,22}(?<=\d)", _options, _timeoutMilliseconds)]
    private static partial Regex IdentityLabel();

    [GeneratedRegex(@"IDMEX[\s\-]*(?:\d[\s\-]?){6,}(?:<+[A-Z0-9<]*)?", _options, _timeoutMilliseconds)]
    private static partial Regex IdentityMachineZone();

    [GeneratedRegex(@"(?<!\d)(?!(?:521|044|045)\d{10})\d{13}(?!\d)", _options, _timeoutMilliseconds)]
    private static partial Regex OcrNumber();

    [GeneratedRegex(@"(?<!\d)(?:\d[ .\-]?){15}\d(?!\d)|(?<!\d)(?:\d[ .\-]?){14}\d(?!\d)", _options, _timeoutMilliseconds)]
    private static partial Regex Card();

    [GeneratedRegex(@"[A-Z0-9._%+\-]+\s{0,2}(?:@|\(AT\)|\[AT\]|\sARROBA\s)\s{0,2}[A-Z0-9\-]+(?:(?:\.|\s+PUNTO\s+|\s?\(PUNTO\)\s?)[A-Z0-9\-]+)+", _options, _timeoutMilliseconds)]
    private static partial Regex Email();

    [GeneratedRegex(
        @"(?<!\d)" + _phonePrefix + @"\(?(?:\d{10}|\d{3}" + _sep + @"\d{3}" + _sep + @"\d{4}|\d{2}" + _sep + @"\d{4}" + _sep + @"\d{4}|\d{2}(?:" + _sepRequired + @"\d{2}){4}"
        + @"|\d{4}" + _sep + @"\d{3}" + _sep + @"\d{3}|\d{3}" + _sep + @"\d{3}" + _sep + @"\d{2}" + _sep + @"\d{2}|\d{3}" + _sep + @"\d{7}|\d{2}" + _sep + @"\d{8}|\d{5}" + _sep + @"\d{5}"
        + @"|(?:\d[ .\-]){9}\d)(?!\d)",
        _options,
        _timeoutMilliseconds)]
    private static partial Regex Phone();

    [GeneratedRegex(@"(?<![\p{L}\d])" + _streetKeyword + @"(?:\s+|(?<=\.)\s*)" + _streetName + @"\s*,?\s*" + _streetNumber + _addressTail, _options, _timeoutMilliseconds)]
    private static partial Regex StreetAddress();

    [GeneratedRegex(@"(?<![\p{L}\d])(?:DOMICILIO|DIRECCI[OÓ]N|UBICACI[OÓ]N|VIVO\s+EN|UBICAD[OA]\s+EN)\s*[:\-]?\s*[^\n.;]{0,60}?\b\d{1,5}(?!\d)" + _units + _addressTail, _options, _timeoutMilliseconds)]
    private static partial Regex LabeledAddress();

    /// <summary>Texto plegado (mayúsculas, ASCII donde se puede) con el mapa a las posiciones del texto original.</summary>
    private sealed class View(string text, int[] starts, int[] ends)
    {
        private static readonly Dictionary<char, char> _homoglyphs = BuildHomoglyphs();

        public string Text { get; } = text;

        public int[] Starts { get; } = starts;

        public int[] Ends { get; } = ends;

        public static View Build(string original, bool keepSeparators)
        {
            var folded = new StringBuilder(original.Length);
            var starts = new int[original.Length + 16];
            var ends = new int[original.Length + 16];
            var count = 0;
            var index = 0;
            foreach (var rune in original.EnumerateRunes())
            {
                var next = index + rune.Utf16SequenceLength;
                if (rune.Value < 0x80 && (rune.Value >= 0x20 || char.IsWhiteSpace((char)rune.Value)))
                {
                    var ascii = char.ToUpperInvariant((char)rune.Value);
                    if (keepSeparators || !IsSeparator(ascii))
                    {
                        folded.Append(ascii);
                        EnsureRoom(ref starts, ref ends, count);
                        starts[count] = index;
                        ends[count++] = next;
                    }

                    index = next;
                    continue;
                }

                foreach (var c in Fold(rune).Where(c => keepSeparators || !IsSeparator(c)))
                {
                    folded.Append(c);
                    EnsureRoom(ref starts, ref ends, count);
                    starts[count] = index;
                    ends[count++] = next;
                }

                index = next;
            }

            return new View(folded.ToString(), starts, ends);
        }

        private static void EnsureRoom(ref int[] starts, ref int[] ends, int count)
        {
            if (count >= starts.Length)
            {
                Array.Resize(ref starts, starts.Length * 2);
                Array.Resize(ref ends, ends.Length * 2);
            }
        }

        private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '-' or '.' or '_' or '/';

        private static IEnumerable<char> Fold(Rune rune)
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (IsInvisible(category, rune))
            {
                return [];
            }

            if (rune.Value < 0x80)
            {
                return [char.ToUpperInvariant((char)rune.Value)];
            }

            if (Rune.IsDigit(rune))
            {
                return [(char)('0' + (int)Rune.GetNumericValue(rune))];
            }

            if (category == UnicodeCategory.DashPunctuation || rune.Value == 0x2212)
            {
                return ['-'];
            }

            return Rune.IsWhiteSpace(rune) ? [' '] : FoldLetter(rune);
        }

        private static bool IsInvisible(UnicodeCategory category, Rune rune) =>
            category is UnicodeCategory.Format or UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
            || (category == UnicodeCategory.Control && !Rune.IsWhiteSpace(rune));

        private static IEnumerable<char> FoldLetter(Rune rune)
        {
            if (rune.Value is >= 0xFF01 and <= 0xFF5E)
            {
                return [char.ToUpperInvariant((char)(rune.Value - 0xFEE0))];
            }

            if (rune.IsBmp && _homoglyphs.TryGetValue((char)rune.Value, out var latin))
            {
                return [latin];
            }

            var decomposed = rune.ToString().Normalize(NormalizationForm.FormKC);
            return decomposed.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).Select(char.ToUpperInvariant);
        }

        private static Dictionary<char, char> BuildHomoglyphs()
        {
            var map = new Dictionary<char, char>();
            AddPairs(map, "АВЕКМНОРСТХУавекмнорстху", "ABEKMHOPCTXYABEKMHOPCTXY");
            AddPairs(map, "ΑΒΕΖΗΙΚΜΝΟΡΤΥΧαβεικνορτυχ", "ABEZHIKMNOPTYXABEIKNOPTYX");
            map['K'] = 'K';
            map['І'] = 'I';
            map['і'] = 'I';
            map['Ј'] = 'J';
            map['ј'] = 'J';
            return map;
        }

        private static void AddPairs(Dictionary<char, char> map, string from, string to)
        {
            foreach (var (key, value) in from.Zip(to))
            {
                map[key] = value;
            }
        }
    }
}
