namespace WebApi.Endpoints;

using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application.Analysis;
using Application.Analysis.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

public static class ScanEndpoints
{
    private const string ContractVersion = "1";
    private const string RateLimitPolicy = "scan";
    private const int MaxBodyBytes = 4 * 1024;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

    private sealed record ScanRequest(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("offset")] int? Offset,
        [property: JsonPropertyName("interaction_id")] string? InteractionId);

    public static IServiceCollection AddScanServices(this IServiceCollection services)
    {
        services.AddScoped<ScanTextUseCase>();
        services.AddRateLimiter(options => options.AddPolicy(RateLimitPolicy, new ScanRateLimitPolicy()));
        return services;
    }

    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scan", HandleAsync)
            .WithName("ScanText").WithTags("Scan").RequireRateLimiting(RateLimitPolicy);
        return app;
    }

    private static async Task<IResult> HandleAsync([FromServices] ScanTextUseCase scan,
        HttpContext context, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("D");
        context.Response.Headers.CacheControl = "no-store";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Deadline);

        try
        {
            if (context.Request.Headers["X-Contract-Version"].ToString() != ContractVersion)
                return InvalidRequest(requestId);

            var request = await ReadingRequestBody.ReadAsync<ScanRequest>(context.Request, MaxBodyBytes, timeout.Token);
            if (request is null) return InvalidRequest(requestId);

            var result = await scan.ExecuteAsync(request.Text, request.Offset, request.InteractionId, timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            return Results.Ok(new
            {
                data = result,
                meta = new { request_id = requestId, interaction_id = request.InteractionId, contract_version = ContractVersion }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return Error("TIMEOUT", "Tra từ quá thời gian chờ.", requestId, StatusCodes.Status504GatewayTimeout, true);
        }
        catch (ArgumentException) { return InvalidRequest(requestId); }
        catch (AllCapabilitiesFailedException)
        {
            return Error("SERVICE_UNAVAILABLE", "Dịch vụ tra từ chưa khả dụng.", requestId,
                StatusCodes.Status503ServiceUnavailable, true);
        }
        catch (Exception)
        {
            return Error("INTERNAL_ERROR", "Đã xảy ra lỗi nội bộ hệ thống.", requestId,
                StatusCodes.Status500InternalServerError, false);
        }
    }

    private static IResult InvalidRequest(string requestId) =>
        Error("VALIDATION_FAILED", "Yêu cầu quét từ không hợp lệ.", requestId, StatusCodes.Status400BadRequest, false);

    private static IResult Error(string code, string message, string requestId, int status, bool retryable) =>
        Results.Json(new { error_code = code, message, request_id = requestId, retryable }, statusCode: status);

    private sealed class ScanRateLimitPolicy : IRateLimiterPolicy<string>
    {
        public RateLimitPartition<string> GetPartition(HttpContext context)
        {
            var address = context.Connection.RemoteIpAddress;
            if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
            return RateLimitPartition.GetFixedWindowLimiter(address?.ToString() ?? "unknown", _ =>
                new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
        }

        public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => RejectAsync;

        private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            var retrySeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            response.Headers.CacheControl = "no-store";
            response.Headers.RetryAfter = retrySeconds.ToString(CultureInfo.InvariantCulture);
            await response.WriteAsJsonAsync(new
            {
                error_code = "RATE_LIMITED",
                message = "Dịch vụ đang nhận quá nhiều yêu cầu tra từ. Vui lòng đợi và thử lại.",
                request_id = Guid.NewGuid().ToString("D"),
                retryable = true,
                retry_after_seconds = retrySeconds
            }, cancellationToken);
        }
    }
}
