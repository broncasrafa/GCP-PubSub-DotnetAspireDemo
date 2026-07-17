using PubSubAspireDemo.Api.Endpoints;

namespace PubSubAspireDemo.Api.Extensions;

public static class RegisterServices
{
    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        return app;
    }
}
