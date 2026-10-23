using OefenApi.Auth;
using OefenApi.Data;
using OefenApi.Models;
using OefenApi.Settings;

namespace OefenApi.Endpoints;

internal static class AuthEndpoints
{
    public static IResult Login(LoginRequest request, AppDatabase database, AppSettings settings, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new ErrorResponse("Gebruikersnaam en wachtwoord zijn verplicht."));
        }

        StoredUser? user = database.FindUserByUsername(request.Username.Trim());
        if (user is null)
        {
            PasswordHasher.VerifyUnknown(request.Password);
            return Results.Json(
                new ErrorResponse(LoginError(settings, userExists: false)),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return Results.Json(
                new ErrorResponse(LoginError(settings, userExists: true)),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        string token = database.CreateSession(user.Id);
        http.Items[RequestItems.Account] = new Account(user.Id, user.Username, user.Role, user.DisplayName, user.Email);
        return Results.Ok(new LoginResponse(token, "Bearer", 8 * 60 * 60));
    }

    private static string LoginError(AppSettings settings, bool userExists)
    {
        if (settings.SecureMode)
        {
            return "Ongeldige inloggegevens.";
        }

        return userExists ? "Onjuist wachtwoord." : "Onbekende gebruiker.";
    }
}
