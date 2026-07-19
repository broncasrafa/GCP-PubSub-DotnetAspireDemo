using System.Text.Json;

using PubSubAspireDemo.PubSub;
using PubSubAspireDemo.PubSub.Contracts;
using PubSubAspireDemo.PubSub.Extensions;
using PubSubAspireDemo.PubSub.Push;

namespace PubSubAspireDemo.Api.Endpoints;

public static class DefaultPushEndpoints
{
    public static IEndpointRouteBuilder MapDefaultPushEndpoints(this IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("api/aspire/pubsub/push").WithTags("PublicarMensagens-Push");

        routes.MapGet("/", ApiInfo)
            .WithName("InfoPush")
            .WithDescription("Endpoint default com as informações do modo PUSH")
            .WithSummary("Endpoint default com as informações do modo PUSH");

        routes.MapPost("/pedidos/criados", ConsumeMessage)
            .WithName("ReceberPedidoCriadoViaPubSubPush")
            .WithDescription("Endpoint para publicar mensagem customizada")
            .WithSummary("Endpoint para publicar mensagem customizada");

        routes.MapPost("/pedidos/publicar-fake", PublishFakeMessage)
            .WithName("PublicarPedidoCriadoPushFake")
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
                    "POST /api/aspire/pubsub/pull/pedidos/publicar",
                    "POST /api/aspire/pubsub/pull/pedidos/publicar-fake"
                }
        });

    private async static Task<IResult> ConsumeMessage(PubSubPushEnvelope envelope, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (envelope.Message is null)
            return Results.BadRequest(new { message = "Envelope invalido. Message ausente." });

        if (string.IsNullOrWhiteSpace(envelope.Message.Data))
            return Results.BadRequest(new { message = "Envelope invalido. Message.Data ausente." });

        string json;

        try
        {
            var bytes = Convert.FromBase64String(envelope.Message.Data);
            json = System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return Results.BadRequest(new { message = "Message.Data nao esta em Base64 valido." });
        }

        PedidoCriadoEvent? evento;

        try
        {
            evento = JsonSerializer.Deserialize<PedidoCriadoEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"[ConsumeMessage] Mensagem consumida:\n{evento.ToJsonStr()}");
        }
        catch
        {
            return Results.BadRequest(new { message = "Payload invalido.", json });
        }

        if (evento is null)
            return Results.BadRequest(new { message = "Payload desserializado como null.", json });

        Console.WriteLine();
        Console.WriteLine("Mensagem recebida via Pub/Sub Push.");
        Console.WriteLine($"Subscription: {envelope.Subscription}");
        Console.WriteLine($"MessageId: {envelope.Message.MessageId}");
        Console.WriteLine($"PedidoId: {evento.PedidoId}");
        Console.WriteLine($"ClienteId: {evento.ClienteId}");
        Console.WriteLine($"Valor: {evento.Valor}");
        Console.WriteLine($"CriadoEm: {evento.CriadoEm:O}");
        Console.WriteLine($"Origem: {evento.Origem}");

        await Task.CompletedTask;

        return Results.Ok(new
        {
            message = "Mensagem push processada com sucesso.",
            envelope.Message.MessageId,
            evento
        });
    }

    private async static Task<IResult> PublishFakeMessage(IConfiguration configuration, IWebHostEnvironment environment, CancellationToken cancellationToken)
    {
        var options = configuration.GetSection("PubSub").Get<PubSubOptions>() ?? new PubSubOptions();

        var evento = new PedidoCriadoEvent
        {
            PedidoId = Guid.NewGuid(),
            ClienteId = Guid.NewGuid(),
            Valor = 159.90m,
            CriadoEm = DateTime.UtcNow,
            Origem = "api-minimal-push-fake"
        };

        if (environment.IsDevelopment() && options.UseEmulator)
            await new PubSubPushSeeder(options).RunAsync(cancellationToken);

        var messageId = await new PubSubPushPublisher(options).PublishPedidoCriadoAsync(evento, cancellationToken);

        return Results.Ok(new
        {
            message = "Mensagem fake publicada no topico push com sucesso.",
            messageId,
            topicId = options.PushTopicId,
            subscriptionId = options.PushSubscriptionId,
            pushEndpoint = options.PushEndpoint,
            evento
        });
    }
}
