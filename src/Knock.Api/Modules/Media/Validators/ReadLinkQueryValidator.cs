using FluentValidation;
using Knock.Api.Modules.Media.Dtos;

namespace Knock.Api.Modules.Media.Validators;

internal sealed class ReadLinkQueryValidator : AbstractValidator<ReadLinkQuery>
{
    public ReadLinkQueryValidator()
    {
        RuleFor(x => x.Version)
            .Must(version => version is null or ReadLinkQuery.Original or ReadLinkQuery.Reduced)
            .WithMessage("Must be original or reducida.");
    }
}
