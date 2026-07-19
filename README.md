# PubSubAspireDemo

Projeto de demonstração para simular o **Google Cloud Pub/Sub** localmente usando **.NET Aspire**, **Docker** e o **Pub/Sub Emulator** oficial da Google.

O objetivo é permitir que uma API publique mensagens em um tópico local e que um Worker C# consuma essas mensagens em modo debug, sem depender de credenciais reais da GCP e sem acessar tópicos de produção.

---
# Modo PULL no projeto Aspire

## Objetivo

Este projeto demonstra um cenário comum em ambientes corporativos:

```text
API publica uma mensagem em um tópico Pub/Sub
        |
        v
Worker consome essa mensagem
        |
        v
Breakpoint no Visual Studio
```

Em vez de usar o Pub/Sub real da GCP, o projeto usa o **Pub/Sub Emulator** rodando localmente em container, orquestrado pelo **Aspire AppHost**.

---

## Arquitetura

```text
PubSubAspireDemo
├── src
│   ├── PubSubAspireDemo.AppHost
│   ├── PubSubAspireDemo.Api
│   ├── PubSubAspireDemo.Worker
│   ├── PubSubAspireDemo.Publisher
│   ├── PubSubAspireDemo.PubSub
│   └── PubSubAspireDemo.Contracts
└── scripts
    ├── build.bat
    ├── run-apphost.bat
    ├── publish.bat
    └── call-api-publish-fake.bat
```

---

## Projetos

### `PubSubAspireDemo.AppHost`

Projeto principal do Aspire.

Responsável por subir e orquestrar:

```text
pubsub-emulator
api
worker
```

Ele é o projeto que deve ser executado no Visual Studio.

---

### `PubSubAspireDemo.Api`

API Minimal com endpoints para publicar mensagens no tópico do Pub/Sub Emulator.

Endpoints disponíveis:

```http
GET /
POST /pedidos/publicar
POST /pedidos/publicar-fake
```

A API publica mensagens no tópico:

```text
pedido-criado-topic
```

---

### `PubSubAspireDemo.Worker`

Console Application que consome mensagens da subscription:

```text
pedido-criado-worker-sub
```

Esse é o projeto onde o breakpoint deve ser colocado para validar o consumo da mensagem.

---

### `PubSubAspireDemo.PubSub`

Projeto compartilhado com a infraestrutura Pub/Sub.

Contém:

```text
PubSubOptions
PubSubSeeder
PubSubPublisher
```

Responsabilidades:

```text
- aplicar variáveis de ambiente do emulator
- criar topic
- criar subscription
- publicar mensagens
- configurar clients com EmulatorOnly
- configurar GrpcCoreAdapter
```

---

### `PubSubAspireDemo.Contracts`

Projeto com os contratos de mensagem.

Exemplo:

```csharp
public sealed class PedidoCriadoEvent
{
    public Guid PedidoId { get; init; }

    public Guid ClienteId { get; init; }

    public decimal Valor { get; init; }

    public DateTime CriadoEm { get; init; }

    public string Origem { get; init; } = "aspire-local-debug";
}
```

---

### `PubSubAspireDemo.Publisher`

Console Application separado para publicar mensagens diretamente no tópico.

Foi separado do Worker para evitar lock de build no Windows quando o Worker estiver rodando em debug.

---

## Fluxo principal

```text
Swagger/Postman
        |
        v
PubSubAspireDemo.Api
        |
        v
PubSubPublisher
        |
        v
pedido-criado-topic
        |
        v
pedido-criado-worker-sub
        |
        v
PubSubAspireDemo.Worker
        |
        v
Breakpoint no WorkerConsumer
```

---

## Requisitos

- Windows 10/11
- Visual Studio 2022
- .NET SDK 9
- Docker Desktop ou Rancher Desktop
- Navegador para acessar o Aspire Dashboard e Swagger

---

## Configurações principais

### ProjectId local

```text
local-project
```

### Topic

```text
pedido-criado-topic
```

### Subscription

```text
pedido-criado-worker-sub
```

### Pub/Sub Emulator Host

```text
127.0.0.1:8085
```

---

## AppHost

Arquivo:

```text
src/PubSubAspireDemo.AppHost/Program.cs
```

Trecho principal:

```csharp
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
```

### Observação importante

O endpoint do container usa:

```csharp
isProxied: false
```

Isso evita que o gRPC do Pub/Sub Emulator passe pelo proxy do Aspire, o que poderia causar erro de handshake HTTP/2.

---

## Por que usar `127.0.0.1` em vez de `localhost`?

Foi adotado:

```text
127.0.0.1:8085
```

para evitar problemas de resolução IPv6/IPv4 no Windows.

---

## Por que usar `GrpcCoreAdapter`?

O projeto usa:

```csharp
GrpcAdapter = GrpcCoreAdapter.Instance
```

nos clients da Google.

Isso foi necessário para evitar erro de handshake HTTP/2 ao conectar com o Pub/Sub Emulator em ambiente local usando Aspire.

Exemplo:

```csharp
var publisherService = await new PublisherServiceApiClientBuilder
{
    EmulatorDetection = _options.UseEmulator
        ? EmulatorDetection.EmulatorOnly
        : EmulatorDetection.ProductionOnly,
    GrpcAdapter = GrpcCoreAdapter.Instance
}.BuildAsync(cancellationToken);
```

---

## Por que usar `EmulatorOnly`?

Quando `UseEmulator = true`, o projeto usa:

```csharp
EmulatorDetection.EmulatorOnly
```

Isso garante que o client só conecte no emulator local.

Se o emulator não estiver rodando, a aplicação falha. Isso é intencional.

Esse comportamento evita o risco de, durante um debug local, a aplicação tentar acessar o Pub/Sub real da GCP.

---

## Como executar pelo Visual Studio

### 1. Abrir a solution

Abra:

```text
PubSubAspireDemo.sln
```

### 2. Definir Startup Project

Defina como Startup Project:

```text
PubSubAspireDemo.AppHost
```

### 3. Executar

Pressione:

```text
F5
```

### 4. Validar Aspire Dashboard

O Aspire Dashboard deve abrir no navegador.

Os recursos esperados são:

```text
pubsub-emulator    Running
api                Running
worker             Running
```

---

## Como executar pelo terminal

Na raiz da solution:

```cmd
dotnet build
```

Depois:

```cmd
dotnet run --project .\src\PubSubAspireDemo.AppHost\PubSubAspireDemo.AppHost.csproj
```

O terminal deve exibir a URL do Aspire Dashboard.

---

## Configuração
- ProjectId: `local-project`
- Topic: `pedido-criado-topic` 
- Subscription: `pedido-criado-worker-sub`
- Emulator: `localhost:8085`
---

## Swagger

Com o AppHost rodando, acesse:

```text
https://localhost:7044/swagger
```

Ou clique no endpoint da API pelo Aspire Dashboard.

---

## Endpoints da API

### Health/root

```http
GET /
```

Resposta esperada:

```json
{
  "application": "PubSubAspireDemo.Api",
  "status": "running",
  "endpoints": [
    "POST /pedidos/publicar",
    "POST /pedidos/publicar-fake"
  ]
}
```

---

### Publicar mensagem customizada

```http
POST /pedidos/publicar
```

Body:

```json
{
  "pedidoId": "11111111-1111-1111-1111-111111111111",
  "clienteId": "22222222-2222-2222-2222-222222222222",
  "valor": 250.75,
  "origem": "swagger-api"
}
```

Resposta esperada:

```json
{
  "message": "Mensagem publicada no tópico com sucesso.",
  "messageId": "1",
  "topicId": "pedido-criado-topic",
  "subscriptionId": "pedido-criado-worker-sub",
  "evento": {
    "pedidoId": "11111111-1111-1111-1111-111111111111",
    "clienteId": "22222222-2222-2222-2222-222222222222",
    "valor": 250.75,
    "criadoEm": "2026-01-01T00:00:00Z",
    "origem": "swagger-api"
  }
}
```

---

### Publicar mensagem fake

```http
POST /pedidos/publicar-fake
```

Esse endpoint gera automaticamente:

```text
PedidoId
ClienteId
Valor
CriadoEm
Origem
```

Exemplo via curl:

```cmd
curl -X POST "https://localhost:7044/pedidos/publicar-fake" ^
  -H "Content-Type: application/json" ^
  -k
```

---

## Script para chamar a API

Arquivo:

```text
scripts/call-api-publish-fake.bat
```

Conteúdo:

```bat
@echo off
setlocal

echo.
echo ==========================================
echo  Chamando API para publicar mensagem fake
echo ==========================================
echo.

curl -X POST "https://localhost:7044/pedidos/publicar-fake" ^
  -H "Content-Type: application/json" ^
  -k

echo.
echo.
pause
```

---

## Como debugar o Worker

### 1. Rode o AppHost

No Visual Studio:

```text
PubSubAspireDemo.AppHost
```

Pressione F5.

### 2. Coloque breakpoint

Arquivo:

```text
src/PubSubAspireDemo.Worker/WorkerConsumer.cs
```

Métodos recomendados:

```csharp
private static async Task<SubscriberClient.Reply> HandleMessageAsync(
    PubsubMessage message,
    CancellationToken cancellationToken)
```

ou:

```csharp
private static Task ProcessarPedidoCriadoAsync(
    PedidoCriadoEvent evento,
    CancellationToken cancellationToken)
```

### 3. Publique uma mensagem

Pelo Swagger:

```http
POST /pedidos/publicar-fake
```

ou pelo script:

```cmd
scripts\call-api-publish-fake.bat
```

### 4. Resultado esperado

O breakpoint deve cair no Worker.

---

## Como publicar sem API

Também é possível publicar pelo projeto console:

```cmd
scripts\publish.bat
```

Esse script executa o projeto:

```text
PubSubAspireDemo.Publisher
```

Fluxo:

```text
Publisher Console
        |
        v
pedido-criado-topic
        |
        v
pedido-criado-worker-sub
        |
        v
Worker
```

---

## Seeder

O projeto cria automaticamente:

```text
Topic: pedido-criado-topic
Subscription: pedido-criado-worker-sub
```

Isso é feito pela classe:

```text
PubSubSeeder
```

O seeder é chamado antes do consumo e antes da publicação.

---

## Retry no Seeder

O `PubSubSeeder` possui retry porque o Worker pode iniciar antes do emulator estar totalmente pronto.

Ele tenta várias vezes antes de falhar definitivamente.

Isso evita erro intermitente na inicialização do Aspire.

---

## Erros comuns e soluções

### Dashboard não abre

Verifique se o Startup Project é:

```text
PubSubAspireDemo.AppHost
```

O Dashboard não abre se você executar diretamente o Worker ou o Publisher.

---

### AppHost sem `launchSettings.json`

O projeto AppHost deve possuir:

```text
src/PubSubAspireDemo.AppHost/Properties/launchSettings.json
```

Exemplo mínimo:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "PubSubAspireDemo.AppHost": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17130;http://localhost:15130",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21030",
        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22057"
      }
    }
  }
}
```

---

### Erro HTTP/2 handshake

Erro típico:

```text
An HTTP/2 connection could not be established because the server did not complete the HTTP/2 handshake.
```

Soluções aplicadas neste projeto:

```text
- usar isProxied: false no endpoint do container
- usar 127.0.0.1:8085
- usar GrpcCoreAdapter
- adicionar retry no PubSubSeeder
```

---

### Erro no Swagger com `Microsoft.OpenApi.Models.OpenApiOperation`

Erro típico:

```text
System.TypeLoadException:
Could not load type 'Microsoft.OpenApi.Models.OpenApiOperation'
```

Solução:

```text
- remover Microsoft.AspNetCore.OpenApi
- remover .WithOpenApi()
- manter somente Swashbuckle.AspNetCore
```

No `.csproj` da API:

```xml
<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
</ItemGroup>
```

No `Program.cs`:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
```

---

### Porta 8085 ocupada

Verifique:

```cmd
netstat -ano | findstr :8085
```

Verifique containers ativos:

```cmd
docker ps
```

Se houver container antigo do Pub/Sub Emulator, pare-o:

```cmd
docker stop ID_DO_CONTAINER
```

---

### Worker não recebe mensagem

Verifique:

```text
1. AppHost está rodando?
2. pubsub-emulator está Running?
3. worker está Running?
4. api está Running?
5. TopicId é pedido-criado-topic?
6. SubscriptionId é pedido-criado-worker-sub?
7. EmulatorHost é 127.0.0.1:8085?
```

---

## Configuração Pub/Sub

Exemplo usado pela API e Worker:

```json
{
  "PubSub": {
    "UseEmulator": true,
    "ProjectId": "local-project",
    "TopicId": "pedido-criado-topic",
    "SubscriptionId": "pedido-criado-worker-sub",
    "EmulatorHost": "127.0.0.1:8085"
  }
}
```

---

## Diferença entre Pull e Push

Este projeto usa o modelo **pull**.

```text
Pull:
    Worker busca mensagens na subscription.

Push:
    Pub/Sub faz POST para um endpoint HTTP.
```

Fluxo atual:

```text
Worker -> Subscription -> Mensagem
```

No modelo push, o consumidor seria um endpoint HTTP chamado pelo Pub/Sub.

---

## Aplicação prática no trabalho

Este modelo pode ser adaptado para uma solution real como:

```text
API.Faturamento.sln
├── API.Faturamento.Api
├── API.Faturamento.Worker
├── API.Faturamento.Application
├── API.Faturamento.Infrastructure
└── API.Faturamento.AppHost
```

Fluxo local:

```text
1. AppHost sobe Pub/Sub Emulator.
2. API roda localmente.
3. Worker roda localmente em debug.
4. API publica mensagem no tópico local.
5. Worker consome do emulator.
6. Breakpoint cai no handler do Worker.
```

Isso permite analisar problemas nos consumidores sem credenciais GCP e sem acessar tópicos reais.

---

## Boas práticas

- Usar `EmulatorOnly` em ambiente local.
- Usar `ProductionOnly` em ambiente produtivo.
- Não usar `EmulatorOrProduction` em ambiente corporativo sensível.
- Separar contratos de mensagem em um projeto próprio.
- Não deixar endpoints de debug ativos em produção.
- Manter o AppHost como orquestrador local.
- Criar topics/subscriptions automaticamente apenas em ambiente local.
- Validar payload real via API antes de debugar Worker.

---

## Comandos úteis

### Build

```cmd
dotnet build
```

### Rodar AppHost

```cmd
dotnet run --project .\src\PubSubAspireDemo.AppHost\PubSubAspireDemo.AppHost.csproj
```

### Publicar mensagem fake pela API

```cmd
scripts\call-api-publish-fake.bat
```

### Publicar mensagem via console Publisher

```cmd
scripts\publish.bat
```

### Ver containers

```cmd
docker ps
```

### Ver porta 8085

```cmd
netstat -ano | findstr :8085
```

---

## Resultado esperado

Ao executar corretamente:

```text
Aspire Dashboard:
    pubsub-emulator    Running
    api                Running
    worker             Running
```

Ao chamar:

```http
POST /pedidos/publicar-fake
```

O Worker deve receber a mensagem e exibir logs parecidos com:

```text
Mensagem recebida. MessageId: 1
Processando PedidoCriadoEvent...
PedidoId: ...
ClienteId: ...
Valor: 159.90
Origem: api-minimal-fake
```

Com breakpoint ativo, a execução deve parar no `WorkerConsumer`.

---

## Conclusão

Este projeto demonstra uma forma segura e produtiva de simular o Pub/Sub localmente com Aspire.

Ele permite:

```text
- rodar Pub/Sub Emulator sem credenciais GCP
- publicar mensagens por API
- consumir mensagens por Worker
- debugar consumidores localmente
- validar contratos de mensagem
- simular fluxo API -> tópico -> Worker
```

Esse modelo é adequado para análise de problemas em workers C# que consomem mensagens Pub/Sub no padrão pull.

--- 

# Modo PUSH no projeto Aspire

Esta seção adiciona o modo **Pub/Sub Push** ao projeto Aspire.

No modo push, o consumidor não é o Worker. O consumidor é um endpoint HTTP da API:

```http
POST /pubsub-push/pedidos/criados
```

## Fluxos disponíveis

### Pull

```text
API
  -> pedido-criado-topic
      -> pedido-criado-worker-sub
          -> Worker
```

### Push

```text
API
  -> pedido-criado-push-topic
      -> pedido-criado-push-sub
          -> POST /pubsub-push/pedidos/criados
```

## Como rodar no Aspire

No Visual Studio, deixe o startup project como:

```text
PubSubAspireDemo.AppHost
```

O AppHost sobe:

```text
pubsub-emulator
api
worker
```

No Aspire, o AppHost funciona como a definição da aplicação distribuída e organiza os recursos e dependências entre eles.

## Como testar Pull

1. Rode o `PubSubAspireDemo.AppHost`.
2. Abra o dashboard do Aspire.
3. Abra a URL da API.
4. Acesse:

```text
http://localhost:5044/swagger
```

5. Coloque breakpoint no Worker:

```text
src/PubSubAspireDemo.Worker/WorkerConsumer.cs
```

6. Execute no Swagger:

```http
POST /pedidos/publicar-fake
```

O breakpoint deve cair no Worker.

## Como testar Push

1. Rode o `PubSubAspireDemo.AppHost`.
2. Abra:

```text
http://localhost:5044/swagger
```

3. Coloque breakpoint na API:

```text
src/PubSubAspireDemo.Api/Program.cs
```

Endpoint:

```http
POST /pubsub-push/pedidos/criados
```

4. Execute no Swagger:

```http
POST /pedidos/publicar-push-fake
```

O Pub/Sub Emulator vai chamar automaticamente:

```http
POST /pubsub-push/pedidos/criados
```

O breakpoint deve cair na API.

## Por que o PushEndpoint usa host.docker.internal?

No Aspire, o Pub/Sub Emulator roda em container e a API roda como processo local.

Então o container precisa chamar uma aplicação que está no host.

Por isso o endpoint configurado para a push subscription é:

```text
http://host.docker.internal:5044/pubsub-push/pedidos/criados
```

## Configuração usada no AppHost

```text
PubSub__PushTopicId=pedido-criado-push-topic
PubSub__PushSubscriptionId=pedido-criado-push-sub
PubSub__PushEndpoint=http://host.docker.internal:5044/pubsub-push/pedidos/criados
PubSub__EmulatorHost=127.0.0.1:8085
```

## Observação

O Worker pode ficar rodando junto no AppHost. Ele não interfere no modo push porque o modo push usa outro tópico e outra subscription.
