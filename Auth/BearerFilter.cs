using System.Net.Http.Headers;
using OefenApi.Data;
using OefenApi.Models;

namespace OefenApi.Auth;

internal sealed class BearerFilter(AppDatabase database) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        if (!AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out AuthenticationHeaderValue? header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Unauthorized(http);
        }

        Account? account = database.FindAccountByToken(header.Parameter.Trim());
        if (account is null)
        {
            return Unauthorized(http);
        }

        http.Items[RequestItems.Account] = account;
        return await next(context);
    }

    private static IResult Unauthorized(HttpContext http)
    {
        http.Response.Headers.WWWAuthenticate = "Bearer";
        return Results.Json(new ErrorResponse("Niet ingelogd."), statusCode: StatusCodes.Status401Unauthorized);
    }
}
