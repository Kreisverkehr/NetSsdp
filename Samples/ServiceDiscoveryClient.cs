#!/usr/bin/env dotnet

#:project ../src/NetSsdp/NetSsdp.csproj
#:package ConsoleTableExt@3.3.0
#:package Microsoft.Extensions.DependencyInjection@10.0.*
#:package Microsoft.Extensions.Logging@10.0.*
#:package Microsoft.Extensions.Logging.Console@10.0.*

using ConsoleTableExt;
using Kreisverkehr.NetSsdp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

IServiceProvider services = new ServiceCollection()
    .AddLogging(b => b
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information)
    )
    .AddSsdp()
    .BuildServiceProvider()
;

var ssdpServiceCollection = services.GetRequiredService<IReadOnlyCollection<SsdpService>>();

Console.WriteLine("Press [ENTER] to exit.");
Console.WriteLine("Press [P] to print currently discovered services.");
Console.WriteLine("Press [D] to discover any media server.");
Console.WriteLine("Press [A] to discover all devices.");
ConsoleKeyInfo key;
while((key = Console.ReadKey()).Key != ConsoleKey.Enter)
{
    Console.Write("\b \b");
    if(key.KeyChar == 'p')
        PrintServices();

    if(key.KeyChar == 'd')
        await DiscoverMediaServers();

    if(key.KeyChar == 'a')
        await DiscoverAll();
}

async Task DiscoverAll()
{
    var client = services.GetRequiredService<ISsdpClient>();
    await client.RunDiscoveryAsync("ssdp:all");
    Console.WriteLine("Discovery finished");
}

async Task DiscoverMediaServers()
{
    var client = services.GetRequiredService<ISsdpClient>();
    await client.RunDiscoveryAsync("urn:schemas-upnp-org:device:MediaServer:1");
    Console.WriteLine("Discovery finished");
}

void PrintServices()
{
    ConsoleTableBuilder
        .From(ssdpServiceCollection
            .Select(s => new ServiceRec(s.Server, s.UniqueServiceName, s.Status.ToString(), s.TimeToLive))
            .ToList()
        )
        .WithTitle("Services")
        .ExportAndWrite();
    Console.WriteLine();
}

record ServiceRec(string? Server, string Usn, string Status, TimeSpan? TimeToLive = null);