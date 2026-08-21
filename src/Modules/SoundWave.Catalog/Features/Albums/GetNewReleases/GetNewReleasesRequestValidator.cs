using FluentValidation;

namespace SoundWave.Catalog.Features.Albums.GetNewReleases;

/// <summary>
/// Validates the new releases query request parameters.
/// </summary>
internal class GetNewReleasesRequestValidator : AbstractValidator<GetNewReleasesRequest>
{
    public GetNewReleasesRequestValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Page index must be 0 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");

        RuleFor(x => x.DaysOld)
            .GreaterThan(0).When(x => x.DaysOld.HasValue)
            .WithMessage("DaysOld must be greater than 0.");
    }
}
