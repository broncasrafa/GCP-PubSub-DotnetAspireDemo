using Microsoft.Extensions.Configuration;
using PubSubAspireDemo.PubSub;
using PubSubAspireDemo.PubSub.Pull;
using PubSubAspireDemo.Worker;

Console.Title = "PubSubAspireDemo.Worker";

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

Console.WriteLine("Worker iniciado.");
Console.WriteLine($"ProjectId: {options.ProjectId}");
Console.WriteLine($"TopicId: {options.TopicId}");
Console.WriteLine($"SubscriptionId: {options.SubscriptionId}");
Console.WriteLine($"UseEmulator: {options.UseEmulator}");
Console.WriteLine($"EmulatorHost: {options.EmulatorHost}");
Console.WriteLine();

await new PubSubSeeder(options).RunAsync(cancellationTokenSource.Token);

var worker = new WorkerConsumer(options);

await worker.RunAsync(cancellationTokenSource.Token);
