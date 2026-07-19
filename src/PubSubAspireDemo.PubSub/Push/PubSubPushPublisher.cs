using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using PubSubAspireDemo.PubSub.Contracts;

namespace PubSubAspireDemo.PubSub.Push;

public sealed class PubSubPushPublisher
{
    private readonly PubSubOptions _options;

    public PubSubPushPublisher(PubSubOptions options) => _options = options;

    public async Task<string> PublishPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken)
    {
        _options.ApplyEnvironmentVariables();

        var topicName = TopicName.FromProjectTopic(_options.ProjectId, _options.PushTopicId);

        var publisher = await new PublisherClientBuilder
        {
            TopicName = topicName,
            EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly
        }.BuildAsync(cancellationToken);

        var json = JsonSerializer.Serialize(evento, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(json) };
        message.Attributes.Add("eventType", nameof(PedidoCriadoEvent));
        message.Attributes.Add("source", evento.Origem);

        var messageId = await publisher.PublishAsync(message);

        Console.WriteLine();
        Console.WriteLine($"[PubSubPushPublisher] Mensagem publicada no topico push com sucesso. MessageId: {messageId}");
        Console.WriteLine(json);

        await publisher.ShutdownAsync(cancellationToken);

        return messageId;
    }
}
