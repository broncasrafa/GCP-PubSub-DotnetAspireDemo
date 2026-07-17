using System.Text.Json;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using PubSubAspireDemo.PubSub;
using PubSubAspireDemo.Worker.Contracts;

namespace PubSubAspireDemo.Worker;

public sealed class WorkerConsumer
{
    private readonly PubSubOptions _options;

    public WorkerConsumer(PubSubOptions options) => _options = options;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _options.ApplyEnvironmentVariables();

        var subscriptionName = SubscriptionName.FromProjectSubscription(_options.ProjectId, _options.SubscriptionId);

        var subscriber = await new SubscriberClientBuilder
        {
            SubscriptionName = subscriptionName,
            EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly,
            GrpcAdapter = GrpcCoreAdapter.Instance
        }.BuildAsync(cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"Worker escutando subscription: {subscriptionName}");
        Console.WriteLine("[WorkerConsumer] Coloque breakpoint no metodo HandleMessageAsync ou ProcessarPedidoCriadoAsync.");
        Console.WriteLine();

        await subscriber.StartAsync(HandleMessageAsync);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[WorkerConsumer] Cancelamento solicitado. Encerrando worker...");
        }
        finally
        {
            await subscriber.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<SubscriberClient.Reply> HandleMessageAsync(PubsubMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(message.Data.ToByteArray());

            Console.WriteLine();
            Console.WriteLine($"[WorkerConsumer] Mensagem recebida. MessageId: {message.MessageId}");
            Console.WriteLine(json);

            var evento = JsonSerializer.Deserialize<PedidoCriadoEvent>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evento is null)
            {
                Console.WriteLine("[WorkerConsumer] Mensagem invalida. Evento desserializado como null.");
                return SubscriberClient.Reply.Nack;
            }

            await ProcessarPedidoCriadoAsync(evento, cancellationToken);

            return SubscriberClient.Reply.Ack;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[WorkerConsumer] Erro ao processar mensagem.");
            Console.WriteLine(ex);

            return SubscriberClient.Reply.Nack;
        }
    }

    private static Task ProcessarPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine($"[WorkerConsumer] Processando PedidoCriadoEvent...");
        Console.WriteLine($"[WorkerConsumer] PedidoId: {evento.PedidoId}");
        Console.WriteLine($"[WorkerConsumer] ClienteId: {evento.ClienteId}");
        Console.WriteLine($"[WorkerConsumer] Valor: {evento.Valor}");
        Console.WriteLine($"[WorkerConsumer] CriadoEm: {evento.CriadoEm:O}");
        Console.WriteLine($"[WorkerConsumer] Origem: {evento.Origem}");

        return Task.CompletedTask;
    }
}