using FluentValidation;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Requests;

namespace SoundWave.SharedKernel.Validators;

public class BasePaginatedRequestValidator : AbstractValidator<BasePaginatedRequest>
{
    public BasePaginatedRequestValidator()
    {
        RuleFor(r => r.PageIndex)
            .GreaterThanOrEqualTo(0)
                .WithMessage("PageIndex must be greater than or equal to 0.");

        RuleFor(r => r.PageSize)
            .InclusiveBetween(SharedConstants.Pagination.MinPageSize, SharedConstants.Pagination.MaxPageSize)
                .WithMessage($"PageSize must be between {SharedConstants.Pagination.MinPageSize} and {SharedConstants.Pagination.MaxPageSize}.");

        RuleFor(r => r.SortDirection)
            .IsInEnum()
                .WithMessage("SortDirection must be 'asc' or 'desc'.");

    }
}

