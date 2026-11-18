namespace WebApi.Endpoints;

using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application.Speech;
using Microsoft.AspNetCore.Mvc;

public static class SpeechEndpoints
{
    private const int MaxRequestBytes = 16 * 1024;
    private const string Version = "1";

    private sealed record Request(
        [property: JsonPropertyName("interaction_id")] string? InteractionId,
        [property: JsonPropertyName("source")] Source? Source);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(SelectionSource), "selection")]
    [JsonDerivedType(typeof(DictionarySource), "dictionary")]
    private abstract record Source;

    private sealed record SelectionSource([property: JsonPropertyName("text")] string? Text) : Source;

    private sealed record DictionarySource(
        [property: JsonPropertyName("canonical_id")] string? CanonicalId,
        [property: JsonPropertyName("reading_id")] long ReadingId,
        [property: JsonPropertyName("written_form_id")] long? WrittenFormId) : Source;

    public static IEndpointRouteBuilder MapSpeechEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/speech", HandleAsync).WithName("ReadJapaneseAloud").WithTags("Speech");
        return app;
    }

    private static async Task<IResult> HandleAsync([FromServices] SpeechUseCase useCase,
        [FromServices] SpeechQuota quota, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var requestId = Guid.NewGuid().ToString("D");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(28));
        try
        {
            if (context.Request.Headers["X-Contract-Version"] != Version) return Invalid(requestId);
            var request = await ReadingRequestBody.ReadAsync<Request>(context.Request, MaxRequestBytes, deadline.Token);
            if (request?.InteractionId is not { Length: 36 } id || !Guid.TryParseExact(id, "D", out _)
                || request.Source is null)
                return Invalid(requestId);

            // EARS[Unwanted] YAU-10: Reject over-quota speech before resolving or sending any text.
            var ip = context.Connection.RemoteIpAddress;
            using var concurrent = quota.TryAcquireConcurrent(ip);
            if (!concurrent.IsAcquired) return RateLimited(requestId);
            using var minute = quota.TryAcquireMinute(ip);
            if (!minute.IsAcquired)
            {
                if (minute.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                return RateLimited(requestId);
            }

            var wav = request.Source switch
            {
                SelectionSource selection => await useCase.ReadSelectionAsync(selection.Text, deadline.Token),
                DictionarySource dictionary => await useCase.ReadDictionaryAsync(dictionary.CanonicalId,
                    dictionary.ReadingId, dictionary.WrittenFormId, deadline.Token),
                _ => throw new ArgumentException("Invalid speech source.")
            };
            deadline.Token.ThrowIfCancellationRequested();

            return Results.Ok(new
            {
                // System.Text.Json encodes byte[] as base64 without a second in-memory string.
                data = new { audio_base64 = wav, mime_type = "audio/wav", language_tag = "ja", source = "ai" },
                meta = new { request_id = requestId, interaction_id = id, contract_version = Version }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return TimedOut(requestId); }
        catch (ArgumentException) { return Invalid(requestId); }
        catch (SpeechProviderException ex)
        {
            return ex.Code switch
            {
                "RATE_LIMITED" => RateLimited(requestId),
                "TIMEOUT" => TimedOut(requestId),
                _ => Error("SERVICE_UNAVAILABLE", ex.AudioTooLong
                    ? "Âm thanh vượt giới hạn. Hãy chọn đoạn ngắn hơn."
                    : "Giọng đọc AI chưa khả dụng. Vui lòng thử lại sau hoặc kiểm tra cấu hình.",
                    requestId, 503, ex.Retryable)
            };
        }
        catch (Exception) { return Error("INTERNAL_ERROR", "Đã xảy ra lỗi nội bộ hệ thống.", requestId, 500, false); }
    }

    private static IResult Invalid(string requestId) =>
        Error("VALIDATION_FAILED", "Nguồn văn bản hoặc yêu cầu phát âm không hợp lệ.", requestId, 400, false);

    private static IResult RateLimited(string requestId) =>
        Error("RATE_LIMITED", "Đã đạt giới hạn phát âm. Vui lòng đợi trước khi thử lại.", requestId, 429, true);

    private static IResult TimedOut(string requestId) =>
        Error("TIMEOUT", "Đã hết thời gian tạo giọng đọc. Vui lòng thử lại.", requestId, 504, true);

    private static IResult Error(string code, string message, string requestId, int status, bool retryable) =>
        Results.Json(new { error_code = code, message, request_id = requestId, retryable }, statusCode: status);
}

public sealed class SpeechQuota : IDisposable
{
    private readonly PartitionedRateLimiter<IPAddress?> concurrent = PartitionedRateLimiter.Create<IPAddress?, string>(
        ip => RateLimitPartition.GetConcurrencyLimiter(PartitionKey(ip), _ => new ConcurrencyLimiterOptions
        {
            PermitLimit = 2,
            QueueLimit = 0
        }));

    private readonly PartitionedRateLimiter<IPAddress?> minute = PartitionedRateLimiter.Create<IPAddress?, string>(
        ip => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ip), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        }));

    public RateLimitLease TryAcquireConcurrent(IPAddress? ip) => concurrent.AttemptAcquire(ip);
    public RateLimitLease TryAcquireMinute(IPAddress? ip) => minute.AttemptAcquire(ip);

    private static string PartitionKey(IPAddress? ip) =>
        (ip?.IsIPv4MappedToIPv6 == true ? ip.MapToIPv4() : ip)?.ToString() ?? "unavailable";

    public void Dispose()
    {
        concurrent.Dispose();
        minute.Dispose();
    }
}
