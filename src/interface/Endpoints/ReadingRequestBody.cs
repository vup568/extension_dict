namespace WebApi.Endpoints;

using System.Text.Json;
using System.Text.Json.Serialization;

internal static class ReadingRequestBody
{
    private static readonly JsonSerializerOptions StrictJson = new()
    {
        AllowDuplicateProperties = false,
        AllowOutOfOrderMetadataProperties = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 8
    };

    public static async Task<T?> ReadAsync<T>(HttpRequest request, int maxBytes, CancellationToken cancellationToken)
        where T : class
    {
        if (!request.HasJsonContentType() || request.ContentLength > maxBytes) return null;

        // EARS[Unwanted] YSC-AC08/YAU-AC03: Bound the actual body even without a Content-Length.
        var buffer = new byte[maxBytes + 1];
        try
        {
            var count = await request.Body.ReadAtLeastAsync(buffer, buffer.Length,
                throwOnEndOfStream: false, cancellationToken: cancellationToken);
            return count <= maxBytes ? JsonSerializer.Deserialize<T>(buffer.AsSpan(0, count), StrictJson) : null;
        }
        catch (JsonException) { return null; }
        // A polymorphic source without its required discriminator cannot be constructed.
        catch (NotSupportedException) { return null; }
        catch (BadHttpRequestException) { return null; }
    }
}
