using PubSubAspireDemo.Api.Models;
using PubSubAspireDemo.PubSub;
using PubSubAspireDemo.PubSub.Contracts;

namespace PubSubAspireDemo.Api.Endpoints;

public static class DefaultEndpoints
{
    public static IEndpointRouteBuilder MapDefaultEndpoints(this IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("api/publish").WithTags("PublicarMensagens");

        routes.MapGet("/", ApiInfo)
            .WithName("Info")
            .WithDescription("Endpoint default com as informações da api")
            .WithSummary("Endpoint default com as informações da api");

        routes.MapPost("/pedidos/publicar", PublishMessage)
            .WithName("PublicarPedidoCriado")
            .WithDescription("Endpoint para publicar mensagem customizada")
            .WithSummary("Endpoint para publicar mensagem customizada");

        routes.MapPost("/pedidos/publicar-fake", PublishFakeMessage)
            .WithName("PublicarPedidoCriadoFake")
            .WithDescription("Endpoint para publicar mensagem fake. Publica uma mensagem sem você precisar mandar body")
            .WithSummary("Endpoint para publicar mensagem fake");

        return routes;
    }

    private async static Task<IResult> ApiInfo()
        => Results.Ok(new
        {
            application = "PubSubAspireDemo.Api",
            status = "running",
            endpoints = new[]
                {
                    "POST /pedidos/publicar",
                    "POST /pedidos/publicar-fake"
                }
        });

    private async static Task<IResult> PublishMessage(PublicarPedidoCriadoRequest request, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var options = configuration.GetSection("PubSub").Get<PubSubOptions>() ?? new PubSubOptions();

        var evento = new PedidoCriadoEvent
        {
            PedidoId = request.PedidoId,
            ClienteId = request.ClienteId,
            Valor = request.Valor,
            CriadoEm = request.CriadoEm ?? DateTime.UtcNow,
            Origem = string.IsNullOrWhiteSpace(request.Origem) ? "api-minimal" : request.Origem
        };

        await new PubSubSeeder(options).RunAsync(cancellationToken);

        var messageId = await new PubSubPublisher(options).PublishPedidoCriadoAsync(evento, cancellationToken);

        return Results.Ok(new
        {
            message = "Mensagem publicada no tópico com sucesso.",
            messageId,
            topicId = options.TopicId,
            subscriptionId = options.SubscriptionId,
            evento
        });
    }

    private async static Task<IResult> PublishFakeMessage(IConfiguration configuration, CancellationToken cancellationToken)
    {
        var options = configuration.GetSection("PubSub").Get<PubSubOptions>() ?? new PubSubOptions();

        var evento = new PedidoCriadoEvent
        {
            PedidoId = Guid.NewGuid(),
            ClienteId = Guid.NewGuid(),
            Valor = 159.90m,
            CriadoEm = DateTime.UtcNow,
            Origem = "api-minimal-fake"
        };

        await new PubSubSeeder(options).RunAsync(cancellationToken);

        var messageId = await new PubSubPublisher(options).PublishPedidoCriadoAsync(evento, cancellationToken);

        return Results.Ok(new
        {
            message = "Mensagem fake publicada no tópico com sucesso.",
            messageId,
            topicId = options.TopicId,
            subscriptionId = options.SubscriptionId,
            evento
        });
    }
}
