using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Kreisverkehr.NetSsdp.Extensions;
using Kreisverkehr.NetSsdp.Model;
using Microsoft.Extensions.Logging;

namespace Kreisverkehr.NetSsdp;

public interface ISsdpServiceCollection : IReadOnlyDictionary<string, SsdpService>, IReadOnlyCollection<SsdpService>;

public class SsdpServiceCollection : ISsdpServiceCollection, IReadOnlyDictionary<string, SsdpService>, IReadOnlyCollection<SsdpService>
{
    private readonly ISsdpClient _ssdpClient;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<SsdpServiceCollection>? _logger;
    private ConcurrentDictionary<string, SsdpService> _ssdpServices = new(StringComparer.InvariantCultureIgnoreCase);

    public IEnumerable<string> Keys => _ssdpServices.Keys;

    public IEnumerable<SsdpService> Values => _ssdpServices.Values;

    public int Count => _ssdpServices.Count;


    public SsdpService this[string key] => _ssdpServices[key];

    public SsdpServiceCollection(ISsdpClient ssdpClient, ILogger<SsdpServiceCollection> logger, ILoggerFactory loggerFactory)
    {
        _ssdpClient = ssdpClient;
        _logger = logger;
        _loggerFactory = loggerFactory;
        SetupMessageProcessing();
    }

    public SsdpServiceCollection(ISsdpClient ssdpClient, ILogger<SsdpServiceCollection> logger)
    {
        _ssdpClient = ssdpClient;
        _logger = logger;
        SetupMessageProcessing();
    }

    public SsdpServiceCollection(ISsdpClient ssdpClient, ILoggerFactory loggerFactory)
    {
        _ssdpClient = ssdpClient;
        _loggerFactory = loggerFactory;
        SetupMessageProcessing();
    }

    public SsdpServiceCollection(ISsdpClient ssdpClient)
    {
        _ssdpClient = ssdpClient;
        SetupMessageProcessing();
    }

    public SsdpServiceCollection() : this(new SsdpClient()) { }

    private void SetupMessageProcessing()
    {
        _ssdpClient.MessageReceived += (_, e) => ProcessMessage(e.Message);
    }

    private void ProcessMessage(SsdpMessage msg)
    {
        if (msg is not ISsdpServiceUpdateMessage updateMsg)
            return;

        ILogger<SsdpService>? svcLogger = _loggerFactory?.CreateLogger<SsdpService>();

        _ssdpServices.AddOrUpdate(updateMsg.UniqueServiceName, (_) => CreateNewService(msg)!, (_, svc) => UpdateService(msg, svc));
    }

    private SsdpService CreateNewService(SsdpMessage msg)
    {
        _logger?.LogNewServiceDiscovered((msg as ISsdpServiceUpdateMessage)?.UniqueServiceName ?? string.Empty);

        return SsdpService.From(msg, _loggerFactory?.CreateLogger<SsdpService>())!;
    }

    private SsdpService UpdateService(SsdpMessage msg, SsdpService svc)
    {
        _logger?.LogServiceUpdated((msg as ISsdpServiceUpdateMessage)?.UniqueServiceName ?? string.Empty);

        return svc.UpdateFrom(msg)!;
    }

    public bool ContainsKey(string key) => _ssdpServices.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out SsdpService value)
        => _ssdpServices.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, SsdpService>> GetEnumerator()
        => _ssdpServices.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<SsdpService> IEnumerable<SsdpService>.GetEnumerator()
        => _ssdpServices.Values.GetEnumerator();
}