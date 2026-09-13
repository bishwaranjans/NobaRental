using System.Text.Json;
using RestEase;

namespace NobaRental.Frontend.Client.Helpers;

#pragma warning disable IDISP004, S3267
internal static class ApiExceptionHelper
{
    public static string GetErrorMessage(Exception ex, string fallback = "An unexpected error occurred.")
    {
        if (ex is ApiException apiEx && !string.IsNullOrWhiteSpace(apiEx.Content))
        {
            return ParseProblemDetails(apiEx.Content, ex.Message);
        }

        return !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : fallback;
    }

    public static string GetErrorMessage(string? responseContent, string fallback = "An unexpected error occurred.")
    {
        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            return ParseProblemDetails(responseContent, fallback);
        }

        return fallback;
    }

    private static string ParseProblemDetails(string content, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
            {
                return detail.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var prop in errors.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var err in prop.Value.EnumerateArray())
                        {
                            if (err.GetString() is { } str)
                            {
                                messages.Add(str);
                            }
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join("; ", messages);
                }
            }

            if (root.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return title.GetString()!;
            }

            return content;
        }
        catch
        {
            return !string.IsNullOrWhiteSpace(content) ? content : fallback;
        }
    }
}
#pragma warning restore IDISP004, S3267
