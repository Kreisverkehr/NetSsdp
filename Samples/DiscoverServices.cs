#!/usr/bin/env dotnet

#:project ../src/NetSsdp/NetSsdp.csproj
#:package Microsoft.Extensions.Logging@10.0.*
#:package Microsoft.Extensions.Logging.Console@10.0.*

using Kreisverkehr.NetSsdp;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
using SsdpClient client = new(loggerFactory.CreateLogger<SsdpClient>());

var services = client.DiscoverAsync();

await foreach(var msg in services)
    Console.WriteLine(msg.UniqueServiceName);