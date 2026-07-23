namespace SoundWave.SharedKernel.Common;

public static class SharedConstants
{
    public const string DBConnectionStringName = "DefaultConnection";
    public const string JwtConfigSectionName = "Jwt";

    public static class Caching
    {
        private const string JwtBlacklistPrefix = "blacklist:jti:";
        public static string GetJwtBlacklistKey(string jti) => $"{JwtBlacklistPrefix}{jti}";
    }

    public static class Pagination
    {
        public const int DefaultPageIndex = 0;
        public const int DefaultPageSize = 10;
        public const int MinPageSize = 1;
        public const int MaxPageSize = 100;
    }
}
