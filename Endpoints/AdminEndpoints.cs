using OefenApi.Auth;
using OefenApi.Data;
using OefenApi.Models;
using OefenApi.Settings;

namespace OefenApi.Endpoints;

internal static class AdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/admin/users/{id:int}", Delete)
            .WithName("DeleteUser")
            .WithTags("Admin")
            .WithSummary("Verwijder een gebruiker.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
    }

    private static IResult Delete(int id, HttpContext http, AppSettings settings, AppDatabase database)
    {
        Account account = AccountContext.Get(http);
        if (settings.SecureMode && !string.Equals(account.Role, "admin", StringComparison.Ordinal))
        {
            return Results.Json(new ErrorResponse("Geen toegang."), statusCode: StatusCodes.Status403Forbidden);
        }

        if (id == account.Id)
        {
            return Results.Json(new ErrorResponse("Je kunt jezelf niet verwijderen."), statusCode: StatusCodes.Status409Conflict);
        }

        if (!database.DeleteUser(id))
        {
            return Results.Json(new ErrorResponse("Niet gevonden."), statusCode: StatusCodes.Status404NotFound);
        }

        return Results.NoContent();
    }
}
