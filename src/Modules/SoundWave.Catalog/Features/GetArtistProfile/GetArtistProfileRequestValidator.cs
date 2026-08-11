using FluentValidation;

namespace SoundWave.Catalog.Features.GetArtistProfile;

internal class GetArtistProfileRequestValidator : AbstractValidator<GetArtistProfileRequest>
{
    public GetArtistProfileRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Artist ID is required.");
    }
}
