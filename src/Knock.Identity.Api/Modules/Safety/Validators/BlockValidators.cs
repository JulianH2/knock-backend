using FluentValidation;
using Knock.Identity.Api.Modules.Safety.Dtos;

namespace Knock.Identity.Api.Modules.Safety.Validators;

/// <summary>Formato de <c>GET /me/bloqueos</c> (spec 043, R-08): <c>limite</c> de 1 a 50; otro valor responde 422. Un cursor ilegible lo rechaza el servicio con 400.</summary>
internal sealed class BlocksQueryValidator : AbstractValidator<BlocksQuery>
{
    public const string FormatReason = "The value is not valid.";

    public BlocksQueryValidator() =>
        RuleFor(x => x.Limite).InclusiveBetween(1, BlocksQuery.MaxLimit).WithMessage(FormatReason);
}

/// <summary>Formato de <c>POST</c> y <c>DELETE /usuarios/{id}/bloqueo</c> (spec 043, R-02 y R-05): <c>id</c> es el identificador público de la persona (un GUID); si no, 422 sin repetir el valor.</summary>
internal sealed class BlockRouteValidator : AbstractValidator<BlockRoute>
{
    public const string FormatReason = "The value is not valid.";

    public BlockRouteValidator() =>
        RuleFor(x => x.Id).Must(value => value is { Length: 36 } && Guid.TryParseExact(value, "D", out _)).WithMessage(FormatReason);
}
