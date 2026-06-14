using System.Threading.Tasks;
using Kreisverkehr.NetSsdp.Model;
using Microsoft.Extensions.Logging;

namespace Kreisverkehr.NetSsdp;

public class SsdpService
{
    private const int DEFAULT_MAX_AGE_SECONDS = 1800;
    private DateTimeOffset _lastUpdate = DateTimeOffset.UtcNow;
    private DateTimeOffset _cacheTimeout = DateTimeOffset.UtcNow.AddSeconds(DEFAULT_MAX_AGE_SECONDS);

    private readonly ILogger<SsdpService>? _logger;

    public Uri? ServiceDescriptionLocation { get; set; }

    public string? Server { get; set; }

    public required string UniqueServiceName { get; set; }

    public SsdpServiceStatus Status => _cacheTimeout > DateTimeOffset.UtcNow ? SsdpServiceStatus.Alive : SsdpServiceStatus.Dead;

    public TimeSpan TimeToLive => _cacheTimeout - DateTimeOffset.UtcNow;

    public SsdpService() { }

    public SsdpService(ILogger<SsdpService>? logger)
    {
        _logger = logger;
    }

    public static SsdpService? From(SsdpMessage msg, ILogger<SsdpService>? logger = null) => msg switch
    {
        SsdpResponseMessage response => FromResponse(response, logger),
        SsdpNotifyMessage notification => FromNotification(notification, logger),
        _ => null
    };

    private static SsdpService FromNotification(SsdpNotifyMessage notification, ILogger<SsdpService>? logger = null)
        => new SsdpService(logger)
        {
            UniqueServiceName = notification.UniqueServiceName
        }.UpdateFromNotification(notification);

    private static SsdpService FromResponse(SsdpResponseMessage response, ILogger<SsdpService>? logger = null)
        => new SsdpService(logger)
        {
            UniqueServiceName = response.UniqueServiceName
        }.UpdateFromResponse(response);

    public SsdpService UpdateFrom(SsdpMessage msg) => msg switch
    {
        SsdpResponseMessage response => UpdateFromResponse(response),
        SsdpNotifyMessage notification => UpdateFromNotification(notification),
        _ => this
    };

    private SsdpService UpdateFromNotification(SsdpNotifyMessage notification)
    {
        ServiceDescriptionLocation = new(notification.Location);
        Server = notification.Server;
        RenewStatusTimer(notification.CacheControl.MaxAge ?? TimeSpan.FromSeconds(DEFAULT_MAX_AGE_SECONDS));
        return this;
    }

    private SsdpService UpdateFromResponse(SsdpResponseMessage response)
    {
        ServiceDescriptionLocation = new(response.Location);
        Server = response.Server;
        RenewStatusTimer(response.CacheControl.MaxAge ?? TimeSpan.FromSeconds(DEFAULT_MAX_AGE_SECONDS));
        return this;
    }

    private async void RenewStatusTimer(TimeSpan timeToUpdate)
    {
        _lastUpdate = DateTimeOffset.UtcNow;
        _cacheTimeout = _lastUpdate.Add(timeToUpdate);
    }
}