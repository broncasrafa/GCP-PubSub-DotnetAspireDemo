using System.Text.Json;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using PubSubAspireDemo.PubSub.Contracts;

namespace PubSubAspireDemo.PubSub;

public sealed class PubSubPublisher
{
    private readonly PubSubOptions _options;

    public PubSubPublisher(PubSubOptions options) => _options = options;

    public async Task<string> PublishPedidoCriadoAsync(CancellationToken cancellationToken)
    {
        var evento = new PedidoCriadoEvent
        {
            PedidoId = Guid.NewGuid(),
            ClienteId = Guid.NewGuid(),
            Valor = 159.90m,
            CriadoEm = DateTime.UtcNow
        };

        return await PublishPedidoCriadoAsync(evento, cancellationToken);
    }

    /// <summary>
    /// Metodo que a API usa para publicar a mensagem recebida pelo endpoint.
    /// </summary>
    /// <param name="evento">objeto para ser publicado no tópico</param>
    /// <param name="cancellationToken">cancellationtoken</param>
    /// <returns>retorna o messageId gerado pelo Pubsub</returns>
    public async Task<string> PublishPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken)
    {
        _options.ApplyEnvironmentVariables();

        var topicName = TopicName.FromProjectTopic(_options.ProjectId, _options.TopicId);

        var publisher = await new PublisherClientBuilder
        {
            TopicName = topicName,
            EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly,
            GrpcAdapter = GrpcCoreAdapter.Instance
        }.BuildAsync(cancellationToken);

        var json = JsonSerializer.Serialize(evento, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(json) };
        message.Attributes.Add("eventType", nameof(PedidoCriadoEvent));
        message.Attributes.Add("source", evento.Origem);

        var messageId = await publisher.PublishAsync(message);

        Console.WriteLine();
        Console.WriteLine($"[PubSub] Mensagem publicada com sucesso. MessageId: {messageId}");
        Console.WriteLine(json);

        await publisher.ShutdownAsync(cancellationToken);

        return messageId;
    }
}
