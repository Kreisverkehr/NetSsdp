using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Kreisverkehr.NetSsdp.Extensions;

namespace Kreisverkehr.NetSsdp.Model;

public class SsdpNotifyMessage : SsdpMessage, ISsdpServiceUpdateMessage
{
    public const string HEADER_LINE = "NOTIFY * HTTP/1.1";

    public CacheControlHeaderValue CacheControl
    {
        get => CacheControlHeaderValue.Parse(GetHeader(SsdpHeader.CACHE_CONTROL));
        set => SetHeader(SsdpHeader.CACHE_CONTROL, value.ToString());
    }

    public string UniqueServiceName
    {
        get => GetHeader(SsdpHeader.UNIQUE_SERVICE_NAME)!;
    }

    public string NotificationType
    {
        get => GetHeader(SsdpHeader.NOTIFICATION_TYPE)!;
    }

    public string NotificationSubType
    {
        get => GetHeader(SsdpHeader.NOTIFICATION_SUB_TYPE)!;
    }

    public string Location
    {
        get => GetHeader(SsdpHeader.LOCATION)!;
    }

    public string Server
    {
        get => GetHeader(SsdpHeader.SERVER)!;
    }

    public SsdpNotifyMessage() : base(HEADER_LINE) { }
}