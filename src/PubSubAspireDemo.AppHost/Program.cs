var builder = DistributedApplication.CreateBuilder(args);

const string projectId = "local-project";
const string emulatorHost = "127.0.0.1:8085";

const string pullTopicId = "pedido-criado-topic";
const string pullSubscriptionId = "pedido-criado-worker-sub";

const string pushTopicId = "pedido-criado-push-topic";
const string pushSubscriptionId = "pedido-criado-push-sub";
const string pushEndpoint = "http://host.docker.internal:5044/api/aspire/pubsub/push/pedidos/criados";

var pubsubEmulator = builder
    .AddContainer("pubsub-emulator", "gcr.io/google.com/cloudsdktool/google-cloud-cli", "emulators")
    .WithArgs(
        "gcloud",
        "beta",
        "emulators",
        "pubsub",
        "start",
        $"--project={projectId}",
        "--host-port=0.0.0.0:8085")
    .WithEndpoint(
        port: 8085,
        targetPort: 8085,
        name: "grpc",
        isProxied: false);

builder
    .AddProject<Projects.PubSubAspireDemo_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBSUB_EMULATOR_HOST", emulatorHost)
    .WithEnvironment("PUBSUB_PROJECT_ID", projectId)
    .WithEnvironment("PubSub__UseEmulator", "true")
    .WithEnvironment("PubSub__ProjectId", projectId)
    .WithEnvironment("PubSub__EmulatorHost", emulatorHost)
    .WithEnvironment("PubSub__TopicId", pullTopicId)
    .WithEnvironment("PubSub__SubscriptionId", pullSubscriptionId)
    .WithEnvironment("PubSub__PushTopicId", pushTopicId)
    .WithEnvironment("PubSub__PushSubscriptionId", pushSubscriptionId)
    .WithEnvironment("PubSub__PushEndpoint", pushEndpoint)
    .WaitFor(pubsubEmulator);

builder
    .AddProject<Projects.PubSubAspireDemo_Worker>("worker")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBSUB_EMULATOR_HOST", emulatorHost)
    .WithEnvironment("PUBSUB_PROJECT_ID", projectId)
    .WithEnvironment("PubSub__UseEmulator", "true")
    .WithEnvironment("PubSub__ProjectId", projectId)
    .WithEnvironment("PubSub__EmulatorHost", emulatorHost)
    .WithEnvironment("PubSub__TopicId", pullTopicId)
    .WithEnvironment("PubSub__SubscriptionId", pullSubscriptionId)
    .WaitFor(pubsubEmulator);

builder.Build().Run();