using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Kreisverkehr.NetSsdp.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = 1000,
        EventName = "SSDP Startup",
        Level = LogLevel.Information,
        Message = "SSDP Client started and listening for messages"
        )]
    internal static partial void LogStartListen(this ILogger logger);

    [LoggerMessage(
        EventId = 1010,
        EventName = "SSDP Packet Received",
        Level = LogLevel.Debug,
        Message = "Received packet from {sender} with {numBytes} bytes"
        )]
    internal static partial void LogPacketReceived(this ILogger logger, IPEndPoint sender, int numBytes);

    [LoggerMessage(
        EventId = 1020,
        EventName = "SSDP Packet Sent",
        Level = LogLevel.Debug,
        Message = "Sent packet with {numBytes} bytes"
        )]
    internal static partial void LogPacketSent(this ILogger logger, int numBytes);

    [LoggerMessage(
        EventId = 1001,
        EventName = "SSDP Packet Contents",
        Level = LogLevel.Trace,
        Message = "Packet contents:\r\n{dataText}"
        )]
    [SuppressMessage("LoggingGenerator", "SYSLIB1015:Argument is not referenced from the logging message", Justification = "data is redundant")]
    internal static partial void LogPacketContent(this ILogger logger, string dataText, byte[] rawData);
    internal static void LogPacketContent(this ILogger logger, byte[] data)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogPacketContent(Encoding.UTF8.GetString(data), data);
    }

    [LoggerMessage(
        EventId = 2000,
        EventName = "SSDP New Service",
        Level = LogLevel.Information,
        Message = "New SSDP Service discovered with USN {usn}"
        )]
    internal static partial void LogNewServiceDiscovered(this ILogger logger, string usn);

    [LoggerMessage(
        EventId = 2100,
        EventName = "SSDP Service Updated",
        Level = LogLevel.Information,
        Message = "SSDP Service with USN {usn} updated"
        )]
    internal static partial void LogServiceUpdated(this ILogger logger, string usn);
}