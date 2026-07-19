using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;

using Grpc.Core;

namespace PubSubAspireDemo.PubSub.Pull;

public sealed class PubSubSeeder
{
    private readonly PubSubOptions _options;

    public PubSubSeeder(PubSubOptions options) => _options = options;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _options.ApplyEnvironmentVariables();

        var topicName = TopicName.FromProjectTopic(_options.ProjectId, _options.TopicId);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_options.ProjectId, _options.SubscriptionId);

        await ExecuteWithRetryAsync(
            async () =>
            {
                var publisherService = await new PublisherServiceApiClientBuilder
                {
                    EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly,
                    GrpcAdapter = GrpcCoreAdapter.Instance
                }.BuildAsync(cancellationToken);

                var subscriberService = await new SubscriberServiceApiClientBuilder
                {
                    EmulatorDetection = _options.UseEmulator ? EmulatorDetection.EmulatorOnly : EmulatorDetection.ProductionOnly,
                    GrpcAdapter = GrpcCoreAdapter.Instance
                }.BuildAsync(cancellationToken);

                await EnsureTopicAsync(publisherService, topicName, cancellationToken);
                await EnsureSubscriptionAsync(subscriberService, subscriptionName, topicName, cancellationToken);
            },
            cancellationToken);
    }

    private static async Task EnsureTopicAsync(
        PublisherServiceApiClient publisherService,
        TopicName topicName,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisherService.CreateTopicAsync(
                new Topic { TopicName = topicName },
                CallSettings.FromCancellationToken(cancellationToken));

            Console.WriteLine($"Topic criado: {topicName.TopicId}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"Topic ja existe: {topicName.TopicId}");
        }
    }

    private static async Task EnsureSubscriptionAsync(
        SubscriberServiceApiClient subscriberService,
        SubscriptionName subscriptionName,
        TopicName topicName,
        CancellationToken cancellationToken)
    {
        try
        {
            await subscriberService.CreateSubscriptionAsync(
                new Subscription
                {
                    SubscriptionName = subscriptionName,
                    TopicAsTopicName = topicName,
                    AckDeadlineSeconds = 60
                },
                CallSettings.FromCancellationToken(cancellationToken));

            Console.WriteLine($"Subscription criada: {subscriptionName.SubscriptionId}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"Subscription ja existe: {subscriptionName.SubscriptionId}");
        }
    }

    private static async Task ExecuteWithRetryAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
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
                Console.WriteLine($"Pub/Sub Emulator ainda nao respondeu corretamente. Tentativa {attempt}/{maximumAttempts}.");
                Console.WriteLine($"StatusCode: {ex.StatusCode}");
                Console.WriteLine(ex.Status.Detail);
                Console.WriteLine("Aguardando 2 segundos...");
                Console.WriteLine();

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (Exception ex) when (attempt < maximumAttempts)
            {
                Console.WriteLine();
                Console.WriteLine($"Falha ao conectar no Pub/Sub Emulator. Tentativa {attempt}/{maximumAttempts}.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Aguardando 2 segundos...");
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
