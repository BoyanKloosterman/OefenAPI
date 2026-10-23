using Microsoft.AspNetCore.Diagnostics;
using OefenApi.Models;
using OefenApi.Settings;

namespace OefenApi.Errors;

internal sealed class JsonErrorHandler(AppSettings settings) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        ErrorResponse body = settings.SecureMode
            ? new ErrorResponse("Er is een fout opgetreden.")
            : new ErrorResponse(exception.ToString());

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }
}
