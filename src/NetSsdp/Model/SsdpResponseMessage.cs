using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Kreisverkehr.NetSsdp.Extensions;

namespace Kreisverkehr.NetSsdp.Model;

public class SsdpResponseMessage : SsdpMessage, ISsdpServiceUpdateMessage
{
    public const string HEADER_LINE = "HTTP/1.1 200 OK";

    public DateTimeOffset? Date
    {
        get
        {
            if (!headerFields.ContainsKey(SsdpHeader.DATE))
                return null;

            return DateTimeOffset.ParseExact(headerFields[SsdpHeader.DATE], "R", CultureInfo.InvariantCulture);
        }
    }

    public CacheControlHeaderValue CacheControl
    {
        get => CacheControlHeaderValue.Parse(GetHeader(SsdpHeader.CACHE_CONTROL));
        set => SetHeader(SsdpHeader.CACHE_CONTROL, value.ToString());
    }

    public string Ext
    {
        get => GetHeader(SsdpHeader.EXT) ?? string.Empty;
    }

    public string Location
    {
        get => GetHeader(SsdpHeader.LOCATION)!;
    }

    public string Server
    {
        get => GetHeader(SsdpHeader.SERVER)!;
    }

    public string UniqueServiceName
    {
        get => GetHeader(SsdpHeader.UNIQUE_SERVICE_NAME)!;
    }

    public string SearchTarget
    {
        get => GetHeader(SsdpHeader.SEARCH_TARGET) ?? string.Empty;
    }

    public SsdpResponseMessage() : base(HEADER_LINE) { }
}