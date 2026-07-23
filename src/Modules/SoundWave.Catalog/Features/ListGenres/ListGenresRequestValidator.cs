using FluentValidation;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.ListGenres;

internal class ListGenresRequestValidator : AbstractValidator<ListGenresRequest>
{
    public ListGenresRequestValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(SharedConstants.Pagination.MinPageSize, SharedConstants.Pagination.MaxPageSize);

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);

        RuleFor(x => x.OrderBy)
            .Must(v => ListGenresRequest.AllowedSortFields.Contains(v))
            .When(x => !string.IsNullOrEmpty(x.OrderBy))
            .WithMessage($"OrderBy must be one of: {string.Join(", ", ListGenresRequest.AllowedSortFields)}.");
    }
}
