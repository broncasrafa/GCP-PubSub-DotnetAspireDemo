# PubSubAspireDemo

Demonstração local de Google Cloud Pub/Sub Emulator usando .NET Aspire.

## Execução

1. Abra `PubSubAspireDemo.sln`.
2. Defina `PubSubAspireDemo.AppHost` como Startup Project.
3. Pressione F5.
4. Coloque breakpoint em `WorkerConsumer.ProcessarPedidoCriadoAsync`.
5. Em outro terminal, rode:

```cmd
scripts\publish.bat
```

## Recursos

- `PubSubAspireDemo.AppHost`: orquestra o Pub/Sub Emulator e o Worker.
- `PubSubAspireDemo.Worker`: consome mensagens via pull.
- `PubSubAspireDemo.Publisher`: publica mensagens locais.
- `PubSubAspireDemo.PubSub`: seeder, options e publisher.
- `PubSubAspireDemo.Contracts`: contrato da mensagem.

## Configuração

- ProjectId: `local-project`
- Topic: `pedido-criado-topic`
- Subscription: `pedido-criado-worker-sub`
- Emulator: `localhost:8085`
