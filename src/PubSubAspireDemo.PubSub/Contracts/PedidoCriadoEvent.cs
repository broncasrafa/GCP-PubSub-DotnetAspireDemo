namespace PubSubAspireDemo.PubSub.Contracts;

public sealed class PedidoCriadoEvent
{
    public Guid PedidoId { get; init; }
    public Guid ClienteId { get; init; }
    public decimal Valor { get; init; }
    public DateTime CriadoEm { get; init; }
    public string Origem { get; init; } = "aspire-local-debug";
}
