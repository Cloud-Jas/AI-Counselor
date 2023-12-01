using ServiceStack.Auth;
using AICounsellorFrontend.Data;

[assembly: HostingStartup(typeof(AICounsellorFrontend.ConfigureAuth))]

namespace AICounsellorFrontend;

public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost => 
        {
            appHost.Plugins.Add(new AuthFeature(IdentityAuth.For<ApplicationUser>(options => {
                options.EnableCredentialsAuth = true;
                options.SessionFactory = () => new CustomUserSession();
            })));
        });
}
