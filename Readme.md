# NetSsdp

NetSsdp is a small .NET library for working with Simple Service Discovery Protocol (SSDP). It provides a lightweight client for sending SSDP search requests, receiving responses and notifications, and parsing SSDP messages.

## Features

- Discover SSDP devices and services using `SsdpClient.DiscoverAsync`
- Send SSDP messages with `SsdpClient.SendMessageAsync`
- Receive SSDP events via `SsdpClient.MessageReceived`
- Parse raw SSDP messages with `SsdpMessage.Parse`
- Dependency injection support through `AddSsdp()` extension method

## Requirements

- .NET 10.0 SDK

## Getting started

### Build the library

From the repository root:

```powershell
dotnet build src\NetSsdp\NetSsdp.csproj
```

### Using the library

You can create an `SsdpClient` directly:

```csharp
using Kreisverkehr.NetSsdp;

using var client = new SsdpClient();

await foreach (var response in client.DiscoverAsync())
{
    Console.WriteLine(response.UniqueServiceName);
}
```

To receive SSDP notifications and messages:

```csharp
client.MessageReceived += (sender, args) =>
{
    Console.WriteLine(args.Message);
};
```

For dependency injection support, add SSDP services to your `IServiceCollection`:

```csharp
services.AddLogging();
services.AddSsdp();

var client = provider.GetRequiredService<ISsdpClient>();
await client.RunDiscoveryAsync("ssdp:all");
```

### Parsing raw SSDP messages

Use `SsdpMessage.Parse` to convert raw SSDP text into a message object:

```csharp
var message = SsdpMessage.Parse(Encoding.UTF8.GetBytes(rawSsdpResponse));
```

## Samples

See `Samples/Readme.md` for descriptions and usage of the included sample programs.

## Project structure

- `src/NetSsdp/` - library source code
- `Samples/` - sample programs demonstrating discovery, message reception, and parsing

## Notes

The library currently targets .NET 10.0.
