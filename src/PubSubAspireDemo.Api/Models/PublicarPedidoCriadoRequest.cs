namespace PubSubAspireDemo.Api.Models;

public sealed class PublicarPedidoCriadoRequest
{
    public Guid PedidoId { get; init; }
    public Guid ClienteId { get; init; }
    public decimal Valor { get; init; }
    public DateTime? CriadoEm { get; init; }
    public string? Origem { get; init; }
}
