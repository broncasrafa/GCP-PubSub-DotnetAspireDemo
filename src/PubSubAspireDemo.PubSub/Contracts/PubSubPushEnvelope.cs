namespace PubSubAspireDemo.PubSub.Contracts;

public sealed class PubSubPushEnvelope
{
    public PubSubPushMessage? Message { get; init; }
    public string? Subscription { get; init; }
}

public sealed class PubSubPushMessage
{
    public string? Data { get; init; }
    public string? MessageId { get; init; }
    public DateTimeOffset? PublishTime { get; init; }
    public Dictionary<string, string>? Attributes { get; init; }
}
