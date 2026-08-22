using Application.Dictionary;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace WebApi.Endpoints;

public static class DictionaryEndpoints
{
    public static IEndpointRouteBuilder MapDictionaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/v1/dictionary/lookup", HandleLookupAsync)
            .WithName("LookupWordV1")
            .WithTags("Dictionary")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status500InternalServerError);

        // Map alias endpoint without /v1 for backwards compatibility
        group.MapGet("/dictionary/lookup", HandleLookupAsync)
            .WithName("LookupWord")
            .WithTags("Dictionary")
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleLookupAsync(
        [FromQuery] string? q,
        [FromServices] LookupWordUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var requestId = httpContext.TraceIdentifier;

        try
        {
            if (q is null)
            {
                return Results.Json(new
                {
                    error_code = "VALIDATION_FAILED",
                    message = "Từ khóa tra cứu không được trống.",
                    request_id = requestId
                }, statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await useCase.ExecuteAsync(q, cancellationToken);

            return Results.Ok(new
            {
                data = result,
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
                message = "Dịch vụ lưu trữ từ điển tạm thời không khả dụng. Vui lòng thử lại sau.",
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
