namespace WebApi.Endpoints;

using Application.Grammar;
using Application.Grammar.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

public static class GrammarEndpoints
{
    public static IEndpointRouteBuilder MapGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/grammar/detect", HandleDetectAsync)
            .WithName("DetectGrammar")
            .WithTags("Grammar")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleDetectAsync(
        [FromBody] GrammarDetectionRequestDto? request,
        [FromServices] DetectGrammarUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var requestId = httpContext.TraceIdentifier;

        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.Json(new
                {
                    error_code = "VALIDATION_FAILED",
                    message = "Nội dung tra cứu không được trống.",
                    request_id = requestId
                }, statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await useCase.ExecuteAsync(request, ct);
            return Results.Ok(new
            {
                data = result.Occurrences,
                meta = new
                {
                    status = result.Status,
                    request_id = requestId,
                    contract_version = "1"
                }
            });
        }
        catch (ArgumentException ex)
        {
            return Results.Json(new
            {
                error_code = "VALIDATION_FAILED",
                message = ex.Message,
                request_id = requestId
            }, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NpgsqlException ex)
        {
            return Results.Json(new
            {
                error_code = "SERVICE_UNAVAILABLE",
                message = $"Dịch vụ lưu trữ dữ liệu PostgreSQL không khả dụng. Lỗi: {ex.Message}",
                request_id = requestId
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            return Results.Json(new
            {
                error_code = "INTERNAL_ERROR",
                message = $"Đã xảy ra lỗi nội bộ hệ thống: {ex.Message}",
                request_id = requestId
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
