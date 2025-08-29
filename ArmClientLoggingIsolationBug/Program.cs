using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

string path1 = $@"{Path.GetTempPath()}\log1.txt";


var sc1 = new ServiceCollection()
    .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddFile(path1))
    .AddSingleton(new DefaultAzureCredential());

sc1.AddAzureClients(builder =>
{
    builder.AddClient<ArmClient, ArmClientOptions>((options, provider) =>
    {
        options.Diagnostics.IsLoggingEnabled = true;
        options.Diagnostics.IsLoggingContentEnabled = true;
        return new ArmClient(provider.GetRequiredService<DefaultAzureCredential>(), defaultSubscriptionId: default, options);
    });
});

using var sp1 = sc1.BuildServiceProvider();

string path2 = $@"{Path.GetTempPath()}\log2.txt";

var sc2 = new ServiceCollection()
    .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddFile(path2))
    .AddSingleton(new DefaultAzureCredential());

sc2.AddAzureClients(builder =>
{
    builder.AddClient<ArmClient, ArmClientOptions>((options, provider) =>
    {
        options.Diagnostics.IsLoggingEnabled = true;
        options.Diagnostics.IsLoggingContentEnabled = true;
        return new ArmClient(provider.GetRequiredService<DefaultAzureCredential>(), defaultSubscriptionId: default, options);
    });
});

using var sp2 = sc2.BuildServiceProvider();

var client1 = sp1.GetRequiredService<ArmClient>();
var logger1 = sp1.GetRequiredService<ILogger<Program>>();
var client2 = sp2.GetRequiredService<ArmClient>();
var logger2 = sp2.GetRequiredService<ILogger<Program>>();

logger1.LogInformation("One");
logger2.LogInformation("Two");
var rg1 = client1.GetResourceGroupResource(new ResourceIdentifier("/subscriptions/3c4db3e5-797b-4aef-809d-1e00373a66e6/resourceGroups/rg1"));
var rg2 = client2.GetResourceGroupResource(new ResourceIdentifier("/subscriptions/3c4db3e5-797b-4aef-809d-1e00373a66e6/resourceGroups/rg2"));

var task1 = rg1.GetAsync();
var task2 = rg2.GetAsync();

try
{
    await Task.WhenAll(task1, task2);
}
catch (Exception) // 404 is expected
{
}

// BUG: both log1 and log2 would contain rg1 and rg2 in them.

Console.WriteLine("Goodbye, World!");