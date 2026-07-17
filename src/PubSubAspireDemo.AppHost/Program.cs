var builder = DistributedApplication.CreateBuilder(args);

const string projectId = "local-project";
const string emulatorHost = "127.0.0.1:8085";

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
    .WithEnvironment("PubSub__TopicId", "pedido-criado-topic")
    .WithEnvironment("PubSub__SubscriptionId", "pedido-criado-worker-sub")
    .WaitFor(pubsubEmulator);

builder
    .AddProject<Projects.PubSubAspireDemo_Worker>("worker")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBSUB_EMULATOR_HOST", emulatorHost)
    .WithEnvironment("PUBSUB_PROJECT_ID", projectId)
    .WithEnvironment("PubSub__UseEmulator", "true")
    .WithEnvironment("PubSub__ProjectId", projectId)
    .WithEnvironment("PubSub__EmulatorHost", emulatorHost)
    .WithEnvironment("PubSub__TopicId", "pedido-criado-topic")
    .WithEnvironment("PubSub__SubscriptionId", "pedido-criado-worker-sub")
    .WaitFor(pubsubEmulator);

builder.Build().Run();