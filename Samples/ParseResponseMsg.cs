#!/usr/bin/env dotnet

#:project ../src/NetSsdp/NetSsdp.csproj

using Kreisverkehr.NetSsdp.Model;
using System.Text;

string sample = """"
        HTTP/1.1 200 OK
        CACHE-CONTROL: max-age=1800
        DATE: Sat, 29 Nov 2025 16:08:56 GMT
        EXT:
        LOCATION: http://192.168.11.12:8008/ssdp/device-desc.xml
        OPT: "http://schemas.upnp.org/upnp/1/0/"; ns=01
        01-NLS: f0318ef8-1dd1-11b2-b673-944bfbb953e5
        SERVER: Linux/4.9.141-tegra-gb6e5605a, UPnP/1.0, Chromecast/1.6.18
        X-User-Agent: redsonic
        ST: urn:dial-multiscreen-org:service:dial:1
        USN: uuid:cf15a03f-2656-d868-40d4-4b7064bd5a8b::urn:dial-multiscreen-org:service:dial:1
        BOOTID.UPNP.ORG: 0
        CONFIGID.UPNP.ORG: 1


        """";

SsdpMessage? message = SsdpMessage.Parse(Encoding.UTF8.GetBytes(sample));
Console.WriteLine("Done");