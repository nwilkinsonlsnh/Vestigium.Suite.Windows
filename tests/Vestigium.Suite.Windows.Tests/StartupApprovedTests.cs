using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class StartupApprovedTests
{
    [Fact]
    public void Missing_flag_is_enabled()
    {
        Assert.True(StartupApproved.IsEnabled(null));
        Assert.True(StartupApproved.IsEnabled(Array.Empty<byte>()));
    }

    [Theory]
    [InlineData(0x02)]
    [InlineData(0x06)]
    public void Enabled_bytes(byte first)
    {
        Assert.True(StartupApproved.IsEnabled(new[] { first, (byte)0, (byte)0, (byte)0 }));
    }

    [Fact]
    public void Disabled_byte()
    {
        Assert.False(StartupApproved.IsEnabled(new byte[] { 0x03, 0, 0, 0 }));
    }

    [Fact]
    public void Hex_text_is_read()
    {
        Assert.False(StartupApproved.IsEnabled("03000000FDC2BA25"));
        Assert.True(StartupApproved.IsEnabled("0200000000000000"));
    }

    [Fact]
    public void Written_bytes_round_trip()
    {
        Assert.True(StartupApproved.IsEnabled(StartupApproved.EnabledBytes()));
        Assert.False(StartupApproved.IsEnabled(StartupApproved.DisabledBytes()));
    }
}
