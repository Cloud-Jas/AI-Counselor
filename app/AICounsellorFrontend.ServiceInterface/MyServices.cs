using ServiceStack;
using AICounsellorFrontend.ServiceModel;

namespace AICounsellorFrontend.ServiceInterface;

public class MyServices : Service
{
    public object Any(Hello request)
    {
        return new HelloResponse { Result = $"Hello, {request.Name}!" };
    }
}