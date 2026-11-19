using FluentValidation;
using Knock.Identity.Api.Modules.Safety.Domain;
using Knock.Identity.Api.Modules.Safety.Dtos;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Safety.Validators;

/// <summary>Formato de <c>POST /reportes</c> (spec 043, R-10 y R-20). Un valor inválido responde 422 con <c>errors.&lt;campo&gt;</c> y nunca repite el valor. Que sea una persona distinta de quien reporta y que exista lo decide el servicio.</summary>
internal sealed class CreateReportBodyValidator : AbstractValidator<CreateReportBody>
{
    public const string RequiredReason = "Required.";
    public const string FormatReason = "The value is not valid.";

    public CreateReportBodyValidator()
    {
        RuleFor(x => x.ReportadoId).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => value is { Length: 36 } && Guid.TryParseExact(value, "D", out _)).WithMessage(FormatReason);
        RuleFor(x => x.Motivo).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => ReportRules.Reasons.Contains(value!, StringComparer.Ordinal)).WithMessage(FormatReason);
        RuleFor(x => x.Descripcion).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(ReportRules.IsValidText).WithMessage(FormatReason);
        RuleFor(x => x.Evidencia)
            .Must(list => list!.Count <= ReportRules.MaxEvidence && list.All(IsImageId) && list.Distinct(StringComparer.Ordinal).Count() == list.Count)
            .WithMessage(FormatReason)
            .When(x => x.Evidencia is not null);
        RuleFor(x => x.Referencia)
            .Must(reference => ReportReferenceKinds.All.Contains(reference!.Tipo ?? string.Empty, StringComparer.Ordinal) && IsImageId(reference.Id))
            .WithMessage(FormatReason)
            .When(x => x.Referencia is not null);
    }

    private static bool IsImageId(string? value) => value is { Length: 24 } && ObjectId.TryParse(value, out _);
}

/// <summary>Formato de <c>GET /reportes/me</c> (spec 043, R-12): <c>limite</c> de 1 a 50. Un cursor ilegible lo rechaza el servicio con 400.</summary>
internal sealed class ReportsQueryValidator : AbstractValidator<ReportsQuery>
{
    public const string FormatReason = "The value is not valid.";

    public ReportsQueryValidator() =>
        RuleFor(x => x.Limite).InclusiveBetween(1, ReportsQuery.MaxLimit).WithMessage(FormatReason);
}

/// <summary>Formato de <c>POST /reportes/{id}/apelacion</c> (spec 043, R-14): <c>id</c> es un <c>ObjectId</c> y <c>texto</c> mide de 8 a 500 caracteres.</summary>
internal sealed class AppealRequestValidator : AbstractValidator<ReportRoute>
{
    public const string FormatReason = "The value is not valid.";

    public AppealRequestValidator() =>
        RuleFor(x => x.Id).Must(value => value is { Length: 24 } && ObjectId.TryParse(value, out _)).WithMessage(FormatReason);
}

/// <summary>Formato del cuerpo de la apelación (spec 043, R-14, R-21).</summary>
internal sealed class AppealBodyValidator : AbstractValidator<AppealBody>
{
    public const string RequiredReason = "Required.";
    public const string FormatReason = "The value is not valid.";

    public AppealBodyValidator() =>
        RuleFor(x => x.Texto).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(ReportRules.IsValidText).WithMessage(FormatReason);
}
