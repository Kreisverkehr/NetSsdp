# Sample Programs

This folder contains sample programs that demonstrate how to use the `NetSsdp` library.

## Samples

### `DiscoverServices.cs`

Discovers SSDP services and prints the `UniqueServiceName` for each discovered response.

- Uses `SsdpClient` directly
- Sends an SSDP search request for `ssdp:all`
- Prints each discovered service as it arrives

### `ListenForMessages.cs`

Listens for incoming SSDP messages and writes the raw message contents to the console.

- Uses `SsdpClient` with logging enabled
- Subscribes to `MessageReceived`
- Keeps running until the user presses a key

### `ParseResponseMsg.cs`

Parses a hardcoded SSDP response message string into an `SsdpMessage` object.

- Demonstrates how to use `SsdpMessage.Parse`
- Uses a sample SSDP response payload
- Prints a completion message after parsing

### `ServiceDiscoveryClient.cs`

Shows dependency injection integration and a simple interactive console app for SSDP discovery. It demonstrates the usage of a readonly collection which is kept up to date througout the lifetime of the application.

- Registers `ISsdpClient` and service collections via `AddSsdp()`
- Prints currently discovered services in a table
- Performs discovery for media servers or all services on-demand

## Running the samples

Each sample is a file-based .NET app. Run a sample from the `Samples` folder using:

```bash
cd Samples
dotnet DiscoverServices.cs
```

## Notes

These samples are intended to help you understand how to discover SSDP devices, receive notifications, and parse SSDP messages using the library.
