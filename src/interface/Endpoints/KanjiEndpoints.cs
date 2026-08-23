using Application.Kanji;
using Application.Kanji.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace WebApi.Endpoints;

public static class KanjiEndpoints
{
    public static IEndpointRouteBuilder MapKanjiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/kanji/lookup", HandleLookupAsync)
            .WithName("LookupKanji")
            .WithTags("Kanji")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleLookupAsync(
        [FromBody] KanjiLookupRequestDto? request,
        [FromServices] LookupKanjiUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
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

            var result = await useCase.ExecuteAsync(request.Text, cancellationToken);

            return Results.Ok(new
            {
                data = result.Matches,
                meta = new
                {
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
        catch (NpgsqlException)
        {
            return Results.Json(new
            {
                error_code = "SERVICE_UNAVAILABLE",
                message = "Dịch vụ lưu trữ dữ liệu Kanji tạm thời không khả dụng. Vui lòng thử lại sau.",
                request_id = requestId
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception)
        {
            return Results.Json(new
            {
                error_code = "INTERNAL_ERROR",
                message = "Đã xảy ra lỗi nội bộ hệ thống.",
                request_id = requestId
            }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
