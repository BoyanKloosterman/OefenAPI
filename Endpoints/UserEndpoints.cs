using OefenApi.Auth;
using OefenApi.Data;
using OefenApi.Models;

namespace OefenApi.Endpoints;

internal static class UserEndpoints
{
    public static void Map(RouteGroupBuilder group, bool secureMode)
    {
        group.MapGet("/users/{id:int}", GetById)
            .WithName("GetUser")
            .WithTags("Users")
            .WithSummary("Haal een gebruiker op.")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        if (secureMode)
        {
            group.MapPut("/users/{id:int}", UpdateOwn)
                .WithName("UpdateUser")
                .WithTags("Users")
                .WithSummary("Werk je gegevens bij.")
                .Accepts<UserProfileUpdate>("application/json")
                .Produces<UserResponse>(StatusCodes.Status200OK)
                .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
                .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
            return;
        }

        group.MapPut("/users/{id:int}", UpdateAny)
            .WithName("UpdateUser")
            .WithTags("Users")
            .WithSummary("Werk een gebruiker bij.")
            .Accepts<UserUpdate>("application/json")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static IResult GetById(int id, HttpContext http, AppDatabase database)
    {
        Account account = AccountContext.Get(http);
        if (id != account.Id)
        {
            return NotFound();
        }

        StoredUser? user = database.GetUser(id);
        if (user is null)
        {
            return NotFound();
        }

        return Results.Ok(ToResponse(user));
    }

    private static IResult UpdateOwn(int id, UserProfileUpdate request, HttpContext http, AppDatabase database)
    {
        return Update(id, request.DisplayName, request.Email, role: null, allowRole: false, requireOwner: true, http, database);
    }

    private static IResult UpdateAny(int id, UserUpdate request, HttpContext http, AppDatabase database)
    {
        return Update(id, request.DisplayName, request.Email, request.Role, allowRole: true, requireOwner: false, http, database);
    }

    private static IResult Update(
        int id,
        string? displayName,
        string? email,
        string? role,
        bool allowRole,
        bool requireOwner,
        HttpContext http,
        AppDatabase database)
    {
        Account account = AccountContext.Get(http);
        if (requireOwner && id != account.Id)
        {
            return NotFound();
        }

        string? nextName = Normalize(displayName);
        string? nextEmail = Normalize(email);
        string? nextRole = allowRole ? Normalize(role) : null;

        if (nextName is null && nextEmail is null && nextRole is null)
        {
            return Results.BadRequest(new ErrorResponse("Geen velden om bij te werken."));
        }

        if (displayName is not null && nextName is null)
        {
            return Results.BadRequest(new ErrorResponse("displayName is leeg."));
        }

        if (nextName is not null && nextName.Length > 100)
        {
            return Results.BadRequest(new ErrorResponse("displayName is te lang."));
        }

        if (email is not null && nextEmail is null)
        {
            return Results.BadRequest(new ErrorResponse("email is leeg."));
        }

        if (nextEmail is not null && !IsEmail(nextEmail))
        {
            return Results.BadRequest(new ErrorResponse("email is ongeldig."));
        }

        if (allowRole && role is not null && nextRole is null)
        {
            return Results.BadRequest(new ErrorResponse("role is leeg."));
        }

        if (nextRole is not null && nextRole is not "user" and not "admin")
        {
            return Results.BadRequest(new ErrorResponse("role is ongeldig."));
        }

        if (database.GetUser(id) is null)
        {
            return NotFound();
        }

        database.UpdateUser(id, nextName, nextEmail, nextRole);
        StoredUser? updated = database.GetUser(id);
        if (updated is null)
        {
            return NotFound();
        }

        return Results.Ok(ToResponse(updated));
    }

    private static string? Normalize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool IsEmail(string value)
    {
        if (value.Length > 200 || value.Contains(' ', StringComparison.Ordinal))
        {
            return false;
        }

        int at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Length - 1;
    }

    private static UserResponse ToResponse(StoredUser user)
    {
        return new UserResponse(user.Id, user.Username, user.Role, user.DisplayName, user.Email);
    }

    private static IResult NotFound()
    {
        return Results.Json(new ErrorResponse("Niet gevonden."), statusCode: StatusCodes.Status404NotFound);
    }
}
