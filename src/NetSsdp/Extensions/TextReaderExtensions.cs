namespace Kreisverkehr.NetSsdp.Extensions;

internal static class TextReaderExtensions
{
    internal static IEnumerable<string> EnumerateLines(this TextReader reader)
    {
        string? line = null;
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }

    internal static async IAsyncEnumerable<string> EnumerateLinesAsync(this TextReader reader)
    {
        string? line = null;
        while ((line = await reader.ReadLineAsync()) != null)
            yield return line;
    }
}