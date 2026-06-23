using System.Net.Sockets;

namespace Kreisverkehr.NetSsdp.Options;

public class SsdpOptions
{
    public bool UseIPv4 { get; set; } = Socket.OSSupportsIPv4;
    public bool UseIPv6 { get; set; } = Socket.OSSupportsIPv6;

}