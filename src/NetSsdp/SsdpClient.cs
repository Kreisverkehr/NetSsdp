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
using Kreisverkehr.NetSsdp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private static readonly IPEndPoint _remoteEndPointV4 = new(IPAddress.Parse("239.255.255.250"), 1900);
    private static readonly IPEndPoint _remoteEndPointV6 = new(IPAddress.Parse("ff05::c"), 1900);

    private readonly UdpClient _multicastClientV4 = new(AddressFamily.InterNetwork);
    private readonly UdpClient _multicastClientV6 = new(AddressFamily.InterNetworkV6);
    private readonly UdpClient _unicastClientV4 = new(AddressFamily.InterNetwork);
    private readonly UdpClient _unicastClientV6 = new(AddressFamily.InterNetworkV6);
    private readonly ILogger<SsdpClient>? _logger;
    private readonly SsdpOptions _options = new();
    private bool _firstUnicastReceiveV4 = true;
    private bool _firstUnicastReceiveV6 = true;

    public event EventHandler<SsdpMessageReceivedEventArgs>? MessageReceived;

    public SsdpClient(ILogger<SsdpClient> logger, IOptions<SsdpOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _logger?.LogInformation("SSDP Client created with options: UseIPv4={UseIPv4}, UseIPv6={UseIPv6}", _options.UseIPv4, _options.UseIPv6);
        SetupNetworking();
    }

    public SsdpClient()
    {
        SetupNetworking();
    }

    private void SetupNetworking()
    {
        if (_options.UseIPv4)
            SetupIpv4Networking();

        if (_options.UseIPv6)
            SetupIpv6Networking();
        
        _logger?.LogStartListen();
    }

    private void SetupIpv4Networking()
    {
        try
        {
            _multicastClientV4.ExclusiveAddressUse = false;
            _multicastClientV4.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _multicastClientV4.Client.Bind(new IPEndPoint(IPAddress.Any, 1900));
            _multicastClientV4.JoinMulticastGroup(_remoteEndPointV4.Address, IPAddress.Any);
            _multicastClientV4.BeginReceive(new AsyncCallback(ReceivedCallback), _multicastClientV4);
        }
        catch (SocketException ex)
        {
            _logger?.LogWarning(ex, "IPv4 multicast discovery could not be started. Continuing with IPv6 only.");
        }
    }

    private void SetupIpv6Networking()
    {
        try
        {
            _multicastClientV6.ExclusiveAddressUse = false;
            _multicastClientV6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _multicastClientV6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, 1900));
            _multicastClientV6.JoinMulticastGroup(_remoteEndPointV6.Address);
            _multicastClientV6.BeginReceive(new AsyncCallback(ReceivedCallback), _multicastClientV6);
        }
        catch (SocketException ex)
        {
            _logger?.LogWarning(ex, "IPv6 multicast discovery could not be started. Continuing with IPv4 only.");
        }
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
        _multicastClientV4.Dispose();
        _multicastClientV6.Dispose();
        _unicastClientV4.Dispose();
        _unicastClientV6.Dispose();
    }

    public async Task SendMessageAsync(SsdpMessage msg, CancellationToken cancellationToken = default)
    {
        var msgData = msg.Format();
        _logger?.LogPacketSent(msgData.Length);
        _logger?.LogPacketContent(msgData);

        EnsureUnicastReceiversStarted();

        if (_options.UseIPv4)
            await _unicastClientV4.SendAsync(msgData, _remoteEndPointV4, cancellationToken);

        if (_options.UseIPv6)
        {
            try
            {
                await _unicastClientV6.SendAsync(msgData, _remoteEndPointV6, cancellationToken);
            }
            catch (SocketException ex)
            {
                _logger?.LogWarning(ex, "IPv6 send failed. Continuing with enabled protocols.");
            }
        }

        if (!_options.UseIPv4 && !_options.UseIPv6)
        {
            _logger?.LogWarning("SendMessageAsync was called but both IPv4 and IPv6 are disabled. No message was sent.");
        }
    }

    public async Task RunDiscoveryAsync(string searchTarget = "ssdp:all", int maxWaitSeconds = 1, bool waitForResponses = false, CancellationToken cancellationToken = default)
    {
        SsdpSearchMessage ipv4Search = new()
        {
            SearchTarget = searchTarget,
            MaxWaitTimeSeconds = maxWaitSeconds
        };

        SsdpSearchMessage ipv6Search = new()
        {
            SearchTarget = searchTarget,
            MaxWaitTimeSeconds = maxWaitSeconds,
            Host = "[ff05::c]:1900"
        };

        EnsureUnicastReceiversStarted();

        if (_options.UseIPv4)
        {
            try
            {
                await _unicastClientV4.SendAsync(ipv4Search.Format(), _remoteEndPointV4, cancellationToken);
            }
            catch (SocketException ex)
            {
                _logger?.LogWarning(ex, "IPv4 discovery failed. Continuing with enabled protocols.");
            }
        }

        if (_options.UseIPv6)
        {
            try
            {
                await _unicastClientV6.SendAsync(ipv6Search.Format(), _remoteEndPointV6, cancellationToken);
            }
            catch (SocketException ex)
            {
                _logger?.LogWarning(ex, "IPv6 discovery failed. Continuing with enabled protocols.");
            }
        }

        if (waitForResponses)
            await Task.Delay(TimeSpan.FromSeconds(maxWaitSeconds), cancellationToken);
    }

    private void EnsureUnicastReceiversStarted()
    {
        if (_options.UseIPv4 && _firstUnicastReceiveV4)
        {
            try
            {
                _unicastClientV4.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                _unicastClientV4.BeginReceive(new AsyncCallback(ReceivedCallback), _unicastClientV4);
                _firstUnicastReceiveV4 = false;
            }
            catch (SocketException ex)
            {
                _logger?.LogWarning(ex, "IPv4 unicast response listener could not be started. IPv4 discovery responses will not be received.");
            }
        }

        if (_options.UseIPv6 && _firstUnicastReceiveV6)
        {
            try
            {
                _unicastClientV6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                _unicastClientV6.BeginReceive(new AsyncCallback(ReceivedCallback), _unicastClientV6);
            }
            catch (SocketException ex)
            {
                _logger?.LogWarning(ex, "IPv6 unicast response listener could not be started. IPv6 discovery responses will not be received.");
            }

            _firstUnicastReceiveV6 = false;
        }
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
