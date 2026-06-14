#!/usr/bin/env dotnet

#:project ../src/NetSsdp/NetSsdp.csproj
#:package Microsoft.Extensions.Logging@10.0.*
#:package Microsoft.Extensions.Logging.Console@10.0.*

using Kreisverkehr.NetSsdp;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));
using SsdpClient client = new SsdpClient(loggerFactory.CreateLogger<SsdpClient>());

client.MessageReceived += (sender, e) =>
{
    Console.WriteLine(e.Message);
};

Console.WriteLine("Press any key to exit.");
Console.ReadLine();