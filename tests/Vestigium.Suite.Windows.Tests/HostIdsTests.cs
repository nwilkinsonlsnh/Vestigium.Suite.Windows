using Vestigium.Suite.Windows.AppMgmtIQ;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class HostIdsTests
{
    [Fact]
    public void AppMgmtIQ_is_the_host_appid()
    {
        Assert.Equal("AppMgmtIQ", HostIds.AppMgmtIQ);
    }
}
