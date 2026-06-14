using System.Net;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Kreisverkehr.NetSsdp.Model;

public interface ISsdpServiceUpdateMessage
{
    string UniqueServiceName { get; }
}

public abstract class SsdpMessage
{
    protected Dictionary<string, string> headerFields { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);

    internal string HeaderLine { get; private set; } = string.Empty;

    public IPEndPoint Sender { get; set; } = new(0, 0);

    protected SsdpMessage(string headerLine)
    {
        HeaderLine = headerLine;
    }

    public static SsdpMessage? Parse(byte[] buffer)
    {
        var data = Encoding.UTF8.GetString(buffer).AsSpan();

        var lineEnumerator = data.EnumerateLines();
        lineEnumerator.MoveNext();
        var msgHeader = lineEnumerator.Current;

        SsdpMessage? result = msgHeader switch
        {
            SsdpResponseMessage.HEADER_LINE => new SsdpResponseMessage(),
            SsdpSearchMessage.HEADER_LINE => new SsdpSearchMessage(),
            SsdpNotifyMessage.HEADER_LINE => new SsdpNotifyMessage(),
            _ => null
        };

        result?.ParseHeader(lineEnumerator);
        return result;
    }

    protected virtual void ParseHeader(SpanLineEnumerator lineEnumerator)
    {
        Span<Range> headerParts = stackalloc Range[2];
        while (lineEnumerator.MoveNext() && lineEnumerator.Current.Length > 0)
        {
            lineEnumerator.Current.Split(headerParts, ':', StringSplitOptions.TrimEntries);
            SetHeader(lineEnumerator.Current[headerParts[0]].ToString(), lineEnumerator.Current[headerParts[1]].ToString());
        }
    }

    protected virtual string? GetHeader(string header)
    {
        if (!headerFields.ContainsKey(header))
            return null;

        return headerFields[header];
    }

    protected virtual void SetHeader(string header, string? value)
    {
        if (!headerFields.ContainsKey(header) && value != null)
        {
            headerFields.Add(header, value);
        }
        else
        {
            if (value == null)
                headerFields.Remove(header);
            else
                headerFields[header] = value;
        }
    }

    public virtual byte[] Format()
    {
        StringBuilder sb = new();
        sb.Append(HeaderLine);
        sb.Append("\r\n");
        foreach (var header in headerFields)
        {
            sb.Append(header.Key);
            sb.Append(": ");
            sb.Append(header.Value);
            sb.Append("\r\n");
        }
        // sb.Append("\r\n");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public override string ToString() => Encoding.UTF8.GetString(Format());
}