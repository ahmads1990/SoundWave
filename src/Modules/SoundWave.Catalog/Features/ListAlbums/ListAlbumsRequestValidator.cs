using FluentValidation;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.ListAlbums;

/// <summary>
/// Validates the paginated album list request parameters.
/// </summary>
internal class ListAlbumsRequestValidator : AbstractValidator<ListAlbumsRequest>
{
    public ListAlbumsRequestValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Page index must be 0 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.OrderBy)
            .Must(ob => ob is null || ListAlbumsRequest.AllowedSortFields.Contains(ob, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"OrderBy must be one of: {string.Join(", ", ListAlbumsRequest.AllowedSortFields)}.");
    }
}
