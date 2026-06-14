using System.Globalization;
using System.Text;
using Kreisverkehr.NetSsdp.Extensions;

namespace Kreisverkehr.NetSsdp.Model;

public class SsdpSearchMessage : SsdpMessage
{
    public const string HEADER_LINE = "M-SEARCH * HTTP/1.1";

    public string SearchTarget
    {
        get => GetHeader(SsdpHeader.SEARCH_TARGET) ?? string.Empty;
        set => SetHeader(SsdpHeader.SEARCH_TARGET, value);
    }

    public int MaxWaitTimeSeconds
    {
        get => int.Parse(GetHeader(SsdpHeader.MX) ?? "1");
        set
        {
            int val = value;
            if(val < 1)
                val = 1;
            if(val > 5)
                val = 5;
            SetHeader(SsdpHeader.MX, val.ToString());
        }
    }

    public string? UserAgent
    {
        get => GetHeader(SsdpHeader.USER_AGENT);
        set => SetHeader(SsdpHeader.USER_AGENT, value);
    }

    public SsdpSearchMessage() : base(HEADER_LINE)
    {
        headerFields.Add(SsdpHeader.HOST, "239.255.255.250:1900");
        headerFields.Add("MAN", "\"ssdp:discover\"");
        headerFields.Add(SsdpHeader.MX, "5");
        headerFields.Add(SsdpHeader.SEARCH_TARGET, "ssdp:all");
    }
}