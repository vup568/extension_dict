namespace WebApi.Endpoints;

using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Translation;
using Microsoft.AspNetCore.Mvc;

public static class TranslationEndpoints
{
    private sealed record Request(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("target_language")] string? TargetLanguage,
        [property: JsonPropertyName("interaction_id")] string? InteractionId);

    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/translations", HandleAsync).WithName("TranslateText").WithTags("Translation");
        return app;
    }

    private static async Task<IResult> HandleAsync([FromServices] TranslationUseCase useCase,
        HttpContext context, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("D");
        try
        {
            Request? request;
            try { request = await context.Request.ReadFromJsonAsync<Request>(cancellationToken); }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
            { return Error("VALIDATION_FAILED", "Yêu cầu dịch không hợp lệ.", requestId, 400, false); }
            if (request is null) return Error("VALIDATION_FAILED", "Yêu cầu dịch không hợp lệ.", requestId, 400, false);
            var translated = await useCase.ExecuteAsync(request.Text, request.TargetLanguage, request.InteractionId, cancellationToken);
            return Results.Ok(new
            {
                data = new { source_language = "ja", target_language = "en", translated_text = translated },
                meta = new { request_id = requestId, interaction_id = request.InteractionId }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ArgumentException) { return Error("VALIDATION_FAILED", "Yêu cầu dịch không hợp lệ.", requestId, 400, false); }
        catch (TranslationProviderException ex)
        {
            var status = ex.Code switch { "TIMEOUT" => 504, "RATE_LIMITED" => 429, _ => 503 };
            return Error(ex.Code, "Dịch vụ dịch chưa khả dụng. Vui lòng kiểm tra cấu hình hoặc thử lại sau.", requestId, status, ex.Retryable);
        }
        catch (Exception) { return Error("INTERNAL_ERROR", "Đã xảy ra lỗi nội bộ hệ thống.", requestId, 500, false); }
    }

    private static IResult Error(string code, string message, string requestId, int status, bool retryable) =>
        Results.Json(new { error_code = code, message, request_id = requestId, retryable }, statusCode: status);
}
