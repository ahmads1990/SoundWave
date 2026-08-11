using FluentValidation;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.ListArtistAccountApprovals;

internal class ListArtistAccountApprovalsRequestValidator : AbstractValidator<ListArtistAccountApprovalsRequest>
{
    public ListArtistAccountApprovalsRequestValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(SharedConstants.Pagination.MinPageSize, SharedConstants.Pagination.MaxPageSize);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.OrderBy)
            .Must(v => ListArtistAccountApprovalsRequest.AllowedSortFields.Contains(v))
            .When(x => !string.IsNullOrEmpty(x.OrderBy))
            .WithMessage($"OrderBy must be one of: {string.Join(", ", ListArtistAccountApprovalsRequest.AllowedSortFields)}.");
    }
}
