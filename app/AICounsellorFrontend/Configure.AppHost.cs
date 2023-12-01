using Funq;
using AICounsellorFrontend.ServiceInterface;

[assembly: HostingStartup(typeof(AICounsellorFrontend.AppHost))]

namespace AICounsellorFrontend;

public class AppHost : AppHostBase, IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) =>
        {
            // Configure ASP.NET Core IOC Dependencies
        });

    public AppHost() : base("AICounsellorFrontend", typeof(MyServices).Assembly) { }

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });
    }
}
