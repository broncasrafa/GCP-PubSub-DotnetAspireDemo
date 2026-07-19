using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Grpc.Core;

namespace PubSubAspireDemo.PubSub.Push;

public sealed class PubSubPushSeeder
{
    private readonly PubSubOptions _options;

    public PubSubPushSeeder(PubSubOptions options) => _options = options;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _options.ApplyEnvironmentVariables();

        var topicName = TopicName.FromProjectTopic(_options.ProjectId, _options.PushTopicId);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_options.ProjectId, _options.PushSubscriptionId);

        await ExecuteWithRetryAsync(
            async () =>
            {
                var publisherService = await new PublisherServiceApiClientBuilder
                {
                    EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly
                }.BuildAsync(cancellationToken);

                var subscriberService = await new SubscriberServiceApiClientBuilder
                {
                    EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly
                }.BuildAsync(cancellationToken);

                await EnsureTopicAsync(publisherService, topicName, cancellationToken);
                await EnsurePushSubscriptionAsync(subscriberService, subscriptionName, topicName, cancellationToken);
            },
            cancellationToken);
    }

    private static async Task EnsureTopicAsync(PublisherServiceApiClient publisherService, TopicName topicName, CancellationToken cancellationToken)
    {
        try
        {
            await publisherService.CreateTopicAsync(new Topic { TopicName = topicName }, CallSettings.FromCancellationToken(cancellationToken));
            Console.WriteLine($"[PubSubPushSeeder] Push topic criado: {topicName.TopicId}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"[PubSubPushSeeder] Push topic ja existe: {topicName.TopicId}");
        }
    }

    private async Task EnsurePushSubscriptionAsync(SubscriberServiceApiClient subscriberService, SubscriptionName subscriptionName, TopicName topicName, CancellationToken cancellationToken)
    {
        var subscription = new Subscription
        {
            SubscriptionName = subscriptionName,
            TopicAsTopicName = topicName,
            AckDeadlineSeconds = 60,
            PushConfig = new PushConfig
            {
                PushEndpoint = _options.PushEndpoint
            }
        };

        try
        {
            await subscriberService.CreateSubscriptionAsync(subscription, CallSettings.FromCancellationToken(cancellationToken));

            Console.WriteLine($"[PubSubPushSeeder] Push subscription criada: {subscriptionName.SubscriptionId}");
            Console.WriteLine($"[PubSubPushSeeder] Push endpoint: {_options.PushEndpoint}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"[PubSubPushSeeder] Push subscription ja existe: {subscriptionName.SubscriptionId}");
            Console.WriteLine("[PubSubPushSeeder] Recriando push subscription local para garantir endpoint atualizado...");

            await subscriberService.DeleteSubscriptionAsync(subscriptionName, CallSettings.FromCancellationToken(cancellationToken));
            await subscriberService.CreateSubscriptionAsync(subscription, CallSettings.FromCancellationToken(cancellationToken));

            Console.WriteLine($"[PubSubPushSeeder] Push subscription recriada: {subscriptionName.SubscriptionId}");
            Console.WriteLine($"[PubSubPushSeeder] Push endpoint: {_options.PushEndpoint}");
        }
    }

    private static async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 30;

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (RpcException ex) when (ShouldRetry(ex) && attempt < maximumAttempts)
            {
                Console.WriteLine();
                Console.WriteLine($"[PubSubPushSeeder] Pub/Sub Emulator ainda nao respondeu corretamente. Tentativa {attempt}/{maximumAttempts}.");
                Console.WriteLine($"[PubSubPushSeeder] StatusCode: {ex.StatusCode}");
                Console.WriteLine(ex.Status.Detail);
                Console.WriteLine("[PubSubPushSeeder] Aguardando 2 segundos...");
                Console.WriteLine();

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (Exception ex) when (attempt < maximumAttempts)
            {
                Console.WriteLine();
                Console.WriteLine($"[PubSubPushSeeder] Falha ao conectar no Pub/Sub Emulator. Tentativa {attempt}/{maximumAttempts}.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("[PubSubPushSeeder] Aguardando 2 segundos...");
                Console.WriteLine();

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        await action();
    }

    private static bool ShouldRetry(RpcException ex)
    {
        return ex.StatusCode == StatusCode.Internal ||
               ex.StatusCode == StatusCode.Unavailable ||
               ex.StatusCode == StatusCode.DeadlineExceeded ||
               ex.StatusCode == StatusCode.Unknown;
    }
}

