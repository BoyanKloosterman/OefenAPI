using OefenApi.Auth;
using OefenApi.Data;
using OefenApi.Models;
using OefenApi.Settings;

namespace OefenApi.Endpoints;

internal static class ClientEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/clients/{id:int}", GetById)
            .WithName("GetClient")
            .WithTags("Clients")
            .WithSummary("Haal een client op.")
            .Produces<ClientResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/clients", Search)
            .WithName("SearchClients")
            .WithTags("Clients")
            .WithSummary("Zoek in je clients.")
            .Produces<ClientListResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    private static IResult GetById(int id, HttpContext http, AppSettings settings, AppDatabase database)
    {
        ClientRow? row = database.GetClient(id);
        if (row is null)
        {
            return NotFound();
        }

        if (settings.SecureMode)
        {
            Account account = AccountContext.Get(http);
            if (row.OwnerUserId != account.Id)
            {
                return NotFound();
            }
        }

        return Results.Ok(ToResponse(row));
    }

    private static IResult Search(string? search, HttpContext http, AppSettings settings, AppDatabase database)
    {
        if (search is not null && search.Length > 2000)
        {
            return Results.BadRequest(new ErrorResponse("search is te lang."));
        }

        Account account = AccountContext.Get(http);
        IReadOnlyList<ClientRow> rows = database.SearchClients(account.Id, search, settings.SecureMode);
        var items = new List<ClientResponse>(rows.Count);
        foreach (ClientRow row in rows)
        {
            items.Add(ToResponse(row));
        }

        return Results.Ok(new ClientListResponse(items));
    }

    private static ClientResponse ToResponse(ClientRow row)
    {
        return new ClientResponse(row.Id, row.OwnerUserId, row.Name, row.Email, row.Note);
    }

    private static IResult NotFound()
    {
        return Results.Json(new ErrorResponse("Niet gevonden."), statusCode: StatusCodes.Status404NotFound);
    }
}
