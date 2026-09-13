using System.Diagnostics.CodeAnalysis;

namespace NobaRental.Backend.WebApi.Helpers;

public static class ETagHelper
{
    public static string? FormatETag(byte[]? rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
        {
            return null;
        }

        return $"\"{Convert.ToBase64String(rowVersion)}\"";
    }

    public static bool TryParseETag(string? etagHeader, [NotNullWhen(true)] out byte[]? rowVersion)
    {
        rowVersion = null;
        if (string.IsNullOrWhiteSpace(etagHeader))
        {
            return false;
        }

        var trimmed = etagHeader.Trim();
        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].Trim();
        }

        trimmed = trimmed.Trim('\"', '\'');

        try
        {
            rowVersion = Convert.FromBase64String(trimmed);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool Matches(string? etagHeader, byte[]? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(etagHeader) || rowVersion is null)
        {
            return false;
        }

        if (string.Equals(etagHeader.Trim(), "*", StringComparison.Ordinal))
        {
            return true;
        }

        return TryParseETag(etagHeader, out var parsed) && rowVersion.AsSpan().SequenceEqual(parsed);
    }

    public static void SetETag(HttpResponse response, byte[]? rowVersion)
    {
        var etag = FormatETag(rowVersion);
        if (!string.IsNullOrEmpty(etag))
        {
            response.Headers.ETag = etag;
        }
    }

    public static bool IsIfNoneMatch(HttpRequest request, byte[]? rowVersion)
    {
        var ifNoneMatch = request.Headers.IfNoneMatch.ToString();
        return !string.IsNullOrWhiteSpace(ifNoneMatch) && Matches(ifNoneMatch, rowVersion);
    }
}
