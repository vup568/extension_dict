namespace WebApi.Endpoints;

using System.Text.Json;
using Application.Analysis;
using Application.Analysis.Exceptions;
using Application.Analysis.Models;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Minimal API endpoint cho Unified Analysis (F-05).
/// POST /api/analysis — Orchestrate vocabulary + kanji + grammar analysis.
/// EARS[Ubiquitous]: Analysis endpoint SHALL NOT require authentication [AC-013].
/// EARS[Ubiquitous]: Translation SHALL NOT được gọi bởi analysis endpoint [TRN-004].
/// </summary>
public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/analysis", HandleAnalyzeAsync)
            .WithName("AnalyzeText")
            .WithTags("Analysis")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAnalyzeAsync(
        [FromServices] AnalyzeTextUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("D");

        try
        {
            AnalysisRequestDto? request;
            try
            {
                request = await httpContext.Request.ReadFromJsonAsync<AnalysisRequestDto>(cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
            {
                return Results.Json(new
                {
                    error_code = "VALIDATION_FAILED",
                    message = "Request body không hợp lệ.",
                    request_id = requestId
                }, statusCode: StatusCodes.Status400BadRequest);
            }

            // Validate request body present
            if (request is null || string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.Json(new
                {
                    error_code = "VALIDATION_FAILED",
                    message = "Nội dung phân tích không được trống.",
                    request_id = requestId
                }, statusCode: StatusCodes.Status400BadRequest);
            }

            // PRIV-002: KHÔNG log request.Text hoặc request.Context
            var result = await useCase.ExecuteAsync(request, cancellationToken);

            // Response envelope theo SPEC §3.4 [AC-001, AC-002, AC-003]
            return Results.Ok(new
            {
                data = result,
                meta = new
                {
                    request_id = requestId,
                    interaction_id = request.InteractionId,
                    contract_version = "1"
                }
            });
        }
        catch (ArgumentException ex)
        {
            // AC-008, AC-009, AC-010, AC-012
            return Results.Json(new
            {
                error_code = "VALIDATION_FAILED",
                message = ex.Message,
                request_id = requestId
            }, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (AllCapabilitiesFailedException)
        {
            // AC-007: Tất cả capabilities thất bại → 503
            return Results.Json(new
            {
                error_code = "SERVICE_UNAVAILABLE",
                message = "Tất cả các dịch vụ phân tích đều không khả dụng. Vui lòng thử lại sau.",
                request_id = requestId,
                retryable = true
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception)
        {
            // SEC-003: Không leak stack trace [AC-012]
            return Results.Json(new
            {
                error_code = "INTERNAL_ERROR",
                message = "Đã xảy ra lỗi nội bộ hệ thống.",
                request_id = requestId
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
