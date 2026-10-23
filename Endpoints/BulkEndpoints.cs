using OefenApi.Data;
using OefenApi.Models;
using OefenApi.Settings;

namespace OefenApi.Endpoints;

internal static class BulkEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/bulkdata", GetPage)
            .WithName("GetBulkData")
            .WithTags("Bulk")
            .WithSummary("Haal een pagina met bulkdata op.")
            .Produces<BulkResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    private static IResult GetPage(int? page, int? pageSize, AppSettings settings, AppDatabase database)
    {
        int resolvedPage = page ?? 1;
        int resolvedSize = pageSize ?? DefaultPageSize;

        if (resolvedPage < 1 || resolvedSize < 1)
        {
            return Results.BadRequest(new ErrorResponse("page en pageSize moeten 1 of hoger zijn."));
        }

        if (settings.SecureMode && resolvedSize > MaxPageSize)
        {
            return Results.BadRequest(new ErrorResponse("pageSize mag maximaal 100 zijn."));
        }

        long offset;
        try
        {
            offset = checked((long)(resolvedPage - 1) * resolvedSize);
        }
        catch (OverflowException)
        {
            return Results.BadRequest(new ErrorResponse("page is te groot."));
        }

        (int total, IReadOnlyList<BulkItemResponse> items) = database.GetBulk(resolvedSize, offset);
        return Results.Ok(new BulkResponse(resolvedPage, resolvedSize, total, items));
    }
}
