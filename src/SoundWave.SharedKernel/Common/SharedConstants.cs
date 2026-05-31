namespace SoundWave.SharedKernel.Common;

public static class SharedConstants
{
    public const string DBConnectionStringName = "DefaultConnection";
    public const string JwtConfigSectionName = "Jwt";

    public static class Caching
    {
        public const string JwtBlacklist = "blacklist:jti:";
    }
}
