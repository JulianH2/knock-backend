using System.Globalization;
using Knock.Market.Api.Modules.Tecnicos.Dtos;

namespace Knock.Market.Api.Modules.Tecnicos.Domain;

/// <summary>Reglas de horarios y de «Disponible hoy» (spec 046, R-46 y R-47), siempre en la hora de la ciudad (<c>America/Mexico_City</c>).</summary>
internal static class ScheduleRules
{
    public const string CityTimeZone = "America/Mexico_City";

    public static readonly IReadOnlyList<string> Days = ["lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo"];

    /// <summary>Hoy a las 23:59:59 en la hora de la ciudad, en UTC: así «Disponible hoy» se apaga solo a la medianoche (R-46).</summary>
    public static DateTime EndOfCityDayUtc(DateTime nowUtc)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(CityTimeZone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), zone);
        var end = local.Date.AddDays(1).AddSeconds(-1);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(end, DateTimeKind.Unspecified), zone);
    }

    /// <summary>Los siete días cerrados: el horario de quien todavía no guarda uno.</summary>
    public static IReadOnlyList<DayScheduleDto> AllClosed() => [.. Days.Select(d => new DayScheduleDto(d, false, []))];

    public static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time) && value.Length == 5;

    /// <summary>
    /// Lista válida: los siete días, cada uno una vez; abierto con una o dos franjas de inicio anterior al fin y sin traslapes; cerrado sin franjas (R-47).
    /// </summary>
    public static bool IsValid(IReadOnlyList<DayScheduleDto>? schedule) =>
        schedule is { Count: 7 }
        && schedule.All(day => day is not null && day.Dia is not null && Days.Contains(day.Dia))
        && schedule.Select(day => day.Dia).Distinct(StringComparer.Ordinal).Count() == 7
        && schedule.All(IsValidDay);

    private static bool IsValidDay(DayScheduleDto day) => day.Abierto switch
    {
        false => day.Franjas is null || day.Franjas.Count == 0,
        true => day.Franjas is { Count: 1 or 2 } ranges && ValidRanges(ranges),
        _ => false,
    };

    private static bool ValidRanges(IReadOnlyList<TimeRangeDto> ranges)
    {
        var parsed = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var range in ranges)
        {
            if (range is null || !TryParseTime(range.Inicio, out var start) || !TryParseTime(range.Fin, out var end) || start >= end)
            {
                return false;
            }

            parsed.Add((start, end));
        }

        return parsed.Count == 1 || parsed.OrderBy(r => r.Start).Zip(parsed.OrderBy(r => r.Start).Skip(1)).All(pair => pair.First.End <= pair.Second.Start);
    }

    /// <summary>Ordena los días de lunes a domingo y deja las franjas vacías en los días cerrados.</summary>
    public static IReadOnlyList<DayScheduleDto> Normalize(IReadOnlyList<DayScheduleDto> schedule) =>
        [.. schedule.OrderBy(d => Days.ToList().IndexOf(d.Dia!)).Select(d => d.Abierto == true
            ? new DayScheduleDto(d.Dia, true, [.. d.Franjas!.OrderBy(r => r.Inicio, StringComparer.Ordinal)])
            : new DayScheduleDto(d.Dia, false, []))];
}
