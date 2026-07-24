using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.ApplyForArtistAccount;

/// <summary>
/// Handles submitting an application for an artist account in the catalog module.
/// </summary>
internal class ApplyForArtistAccountCommandHandler(
    CatalogDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<ApplyForArtistAccountCommandHandler> logger)
    : IRequestHandler<ApplyForArtistAccountCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        ApplyForArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(cancellationToken);
        if (!validationResult.IsSuccess)
            return validationResult;

        var approval = await CreateAndSaveApplicationAsync(request, cancellationToken);
        return Result<CatalogError, Guid>.Success(approval.Id);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue || currentUserService.UserId.Value == Guid.Empty)
        {
            logger.LogWarning("Artist application rejected — user is not authenticated");
            return Result<CatalogError, Guid>.Failure(CatalogError.UserNotAuthenticated);
        }

        var userId = currentUserService.UserId.Value;
        var exists = await dbContext.ArtistAccountApprovals.AnyAsync(a => a.UserId == userId, cancellationToken);
        if (exists)
        {
            logger.LogWarning("Artist application rejected — user {UserId} already submitted an application", userId);
            return Result<CatalogError, Guid>.Failure(CatalogError.ArtistApplicationAlreadyExists);
        }

        return Result<CatalogError, Guid>.Success(default);
    }

    private async Task<ArtistAccountApproval> CreateAndSaveApplicationAsync(
        ApplyForArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var approval = new ArtistAccountApproval
        {
            UserId = currentUserService.UserId!.Value,
            StageName = request.StageName,
            Bio = request.Bio,
            Status = ArtistApprovalStatus.Pending
        };

        await dbContext.ArtistAccountApprovals.AddAsync(approval, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Artist application {ApprovalId} submitted by user {UserId} with stage name {StageName}", approval.Id, approval.UserId, approval.StageName);
        return approval;
    }

    #endregion
}
