using Microsoft.AspNetCore.Routing;

namespace SoundWave.SharedKernel.Common;

/// <summary>
/// Defines a minimal API endpoint. Implemented per-feature in each module's vertical slice.
/// </summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
