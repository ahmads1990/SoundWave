using FluentAssertions;
using SoundWave.Identity.Services;

namespace SoundWave.Identity.Tests.Services;

public class OtpServiceTests
{
    private static OtpService BuildService() => new OtpService();

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void GenerateOtp_ReturnsCorrectLength(int length)
    {
        var otp = BuildService().GenerateOtp(length);
        otp.Should().HaveLength(length);
    }

    [Fact]
    public void GenerateOtp_ContainsOnlyDigits()
    {
        var otp = BuildService().GenerateOtp(6);
        otp.Should().MatchRegex(@"^\d+$");
    }

    [Fact]
    public void GenerateOtp_ReturnsDifferentValuesEachCall()
    {
        var svc = BuildService();
        var otp1 = svc.GenerateOtp();
        var otp2 = svc.GenerateOtp();
        // This could theoretically collide 1 in 1,000,000 times — acceptable
        otp1.Should().NotBe(otp2);
    }

    [Fact]
    public void GenerateOtp_ZeroLength_Throws()
    {
        var act = () => BuildService().GenerateOtp(0);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*greater than zero*");
    }
}
