using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Kreisverkehr.NetSsdp.Extensions;
using Kreisverkehr.NetSsdp.Model;
using Microsoft.Extensions.Logging;

namespace Kreisverkehr.NetSsdp;

public interface ISsdpClient : IDisposable
{
    event EventHandler<SsdpMessageReceivedEventArgs>? MessageReceived;

    Task SendMessageAsync(SsdpMessage msg, CancellationToken cancellationToken = default);

    IAsyncEnumerable<SsdpResponseMessage> DiscoverAsync(string searchTarget = "ssdp:all", int maxWaitSeconds = 1, CancellationToken cancellationToken = default);

    Task RunDiscoveryAsync(string searchTarget = "ssdp:all", int maxWaitSeconds = 1, bool waitForResponses = false, CancellationToken cancellationToken = default);
}

public class SsdpMessageReceivedEventArgs : EventArgs
{
    public required SsdpMessage Message { get; set; }
}

public class SsdpClient : ISsdpClient, IDisposable
{
    private readonly IPEndPoint _remoteEndPoint = new(IPAddress.Parse("239.255.255.250"), 1900);
    private readonly UdpClient _multicastClient = new();
    private readonly UdpClient _unicastCLient = new(AddressFamily.InterNetwork);
    private readonly ILogger<SsdpClient>? _logger;
    private bool _firstDiscovery = true;

    public event EventHandler<SsdpMessageReceivedEventArgs>? MessageReceived;

    public SsdpClient(ILogger<SsdpClient> logger)
    {
        _logger = logger;
        SetupNetworking();
    }

    public SsdpClient()
    {
        SetupNetworking();
    }

    private void SetupNetworking()
    {
        // setup multicast
        _multicastClient.ExclusiveAddressUse = false;
        _multicastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _multicastClient.Client.Bind(new IPEndPoint(IPAddress.Any, 1900));
        _multicastClient.JoinMulticastGroup(_remoteEndPoint.Address, IPAddress.Any);
        _multicastClient.BeginReceive(new AsyncCallback(ReceivedCallback), _multicastClient);
        _logger?.LogStartListen();
    }

    private void ReceivedCallback(IAsyncResult ar)
    {
        if (ar.AsyncState is not UdpClient client) return;

        // Get received data
        IPEndPoint? sender = new IPEndPoint(0, 0);
        byte[] receivedBytes = client.EndReceive(ar, ref sender);
        _logger?.LogPacketReceived(sender!, receivedBytes.Length);
        _logger?.LogPacketContent(receivedBytes);

        // Restart listening for udp data packages
        client.BeginReceive(new AsyncCallback(ReceivedCallback), client);

        Task.Run(ParseAndNotify);

        void ParseAndNotify()
        {
            // If noone is interested in this message, discard it without even bothering to parse it.
            if (MessageReceived == null) return;

            SsdpMessage? msg = SsdpMessage.Parse(receivedBytes);

            // If the message could not be parsed, discard it.
            if (msg == null) return;
            if (sender != null)
                msg.Sender = sender;

            MessageReceived(this, new() { Message = msg });
        }
    }

    public void Dispose()
    {
        _multicastClient.Dispose();
        _unicastCLient.Dispose();
    }

    public async Task SendMessageAsync(SsdpMessage msg, CancellationToken cancellationToken = default)
    {
        var msgData = msg.Format();
        _logger?.LogPacketSent(msgData.Length);
        _logger?.LogPacketContent(msgData);

        await _unicastCLient.SendAsync(msgData, _remoteEndPoint, cancellationToken);
        if (_firstDiscovery)
        {
            _unicastCLient.BeginReceive(new AsyncCallback(ReceivedCallback), _unicastCLient);
            _firstDiscovery = false;
        }
    }

    public async Task RunDiscoveryAsync(string searchTarget = "ssdp:all", int maxWaitSeconds = 1, bool waitForResponses = false, CancellationToken cancellationToken = default)
    {
        SsdpSearchMessage msg = new()
        {
            SearchTarget = searchTarget,
            MaxWaitTimeSeconds = maxWaitSeconds
        };

        await SendMessageAsync(msg, cancellationToken);

        if(waitForResponses)
            await Task.Delay(TimeSpan.FromSeconds(msg.MaxWaitTimeSeconds), cancellationToken);
    }

    public async IAsyncEnumerable<SsdpResponseMessage> DiscoverAsync(string searchTarget = "ssdp:all", int maxWaitSeconds = 1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // create channel to process responses and register a handler to handle received responses
        var channel = Channel.CreateUnbounded<SsdpResponseMessage>();
        MessageReceived += ProcessResponse;

        // send initial search message and setup a trigger to close the channel and stop receiving responses after the configured wait time
        _ = RunDiscoveryAsync(searchTarget, maxWaitSeconds, waitForResponses: true, cancellationToken: cancellationToken)
            .ContinueWith(EndReceivingResponses, CancellationToken.None);

        // process every new message written to the channel and return it. The loop will end as soon as the trigger configured above completes the channel
        while (await channel.Reader.WaitToReadAsync(cancellationToken))
            yield return await channel.Reader.ReadAsync(cancellationToken);

        // helper to process new messages and write them to the channel
        async void ProcessResponse(object? sender, SsdpMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Message is not SsdpResponseMessage response)
                return;

            await channel.Writer.WriteAsync(response);
        }

        // helper to end processing responses and to complete the channel
        void EndReceivingResponses(Task _)
        {
            channel.Writer.Complete();
            MessageReceived -= ProcessResponse;
        }
    }
}
