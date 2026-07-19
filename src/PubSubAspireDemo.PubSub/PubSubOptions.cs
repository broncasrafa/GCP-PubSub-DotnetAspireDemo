using Microsoft.Extensions.Configuration;

namespace PubSubAspireDemo.PubSub;

public sealed class PubSubOptions
{
    public bool UseEmulator { get; set; } = true;
    public string EmulatorHost { get; set; } = "localhost:8085";
    // pull
    public string ProjectId { get; set; } = "local-project";
    public string TopicId { get; set; } = "pedido-criado-topic";
    public string SubscriptionId { get; set; } = "pedido-criado-worker-sub";
    // push
    public string PushTopicId { get; set; } = "pedido-criado-push-topic";
    public string PushSubscriptionId { get; set; } = "pedido-criado-push-sub";
    public string PushEndpoint { get; set; } = "http://localhost:5044/api/aspire/pubsub/push/pedidos/criados";

    public static PubSubOptions Load(IConfiguration configuration)
    {
        var options = new PubSubOptions();
        configuration.GetSection("PubSub").Bind(options);

        var projectFromEnvironment = Environment.GetEnvironmentVariable("PUBSUB_PROJECT_ID");
        if (!string.IsNullOrWhiteSpace(projectFromEnvironment))
            options.ProjectId = projectFromEnvironment;

        var emulatorHostFromEnvironment = Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST");
        if (!string.IsNullOrWhiteSpace(emulatorHostFromEnvironment))
            options.EmulatorHost = emulatorHostFromEnvironment;

        return options;
    }

    public void ApplyEnvironmentVariables()
    {
        if (!UseEmulator)
            return;

        Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", EmulatorHost);
        Environment.SetEnvironmentVariable("PUBSUB_PROJECT_ID", ProjectId);
    }
}
