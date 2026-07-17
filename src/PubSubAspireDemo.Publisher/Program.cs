using Microsoft.Extensions.Configuration;
using PubSubAspireDemo.PubSub;

Console.Title = "PubSubAspireDemo.Publisher";

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var options = PubSubOptions.Load(configuration);

Console.WriteLine($"[PUBLISHER] Publisher iniciado.");
Console.WriteLine($"[PUBLISHER] ProjectId: {options.ProjectId}");
Console.WriteLine($"[PUBLISHER] TopicId: {options.TopicId}");
Console.WriteLine($"[PUBLISHER] UseEmulator: {options.UseEmulator}");
Console.WriteLine($"[PUBLISHER] EmulatorHost: {options.EmulatorHost}");

await new PubSubSeeder(options).RunAsync(cancellationTokenSource.Token);
await new PubSubPublisher(options).PublishPedidoCriadoAsync(cancellationTokenSource.Token);
