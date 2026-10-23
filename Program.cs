using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using OefenApi;
using OefenApi.Auth;
using OefenApi.Data;
using OefenApi.Endpoints;
using OefenApi.Errors;
using OefenApi.Models;
using OefenApi.OpenApi;
using OefenApi.Settings;

// Alleen 127.0.0.1. De API mag niet op het netwerk luisteren.
var builder = WebApplication.CreateBuilder(args);
bool secureMode = builder.Configuration.GetValue<bool>("SecureMode");
var settings = new AppSettings(secureMode);

builder.Services.AddSingleton(settings);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = !settings.SecureMode;
    options.Listen(IPAddress.Loopback, ListenAddress.Port);
});

string dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "oefen.db");
var database = new AppDatabase(dataPath);
database.ResetAndSeed();
builder.Services.AddSingleton(database);

string logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
var requestLog = new RequestLog(logDirectory);
builder.Services.AddSingleton(requestLog);

builder.Services.AddExceptionHandler<JsonErrorHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddTransient<BearerFilter>();
OpenApiSetup.Add(builder.Services, settings);

if (!settings.SecureMode)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });
}
else
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new ErrorResponse("Te veel inlogpogingen. Wacht een minuut."),
                token);
        };
        options.AddFixedWindowLimiter("login", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        });
    });
}

WebApplication app = builder.Build();

app.Use(async (context, next) =>
{
    string requestId = Guid.NewGuid().ToString("D");
    context.Items[RequestItems.RequestId] = requestId;
    context.Response.Headers["X-Request-Id"] = requestId;

    if (settings.SecureMode)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        context.Response.Headers["Permissions-Policy"] = "interest-cohort=()";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        context.Response.Headers["Cache-Control"] = "no-store";
    }

    try
    {
        await next(context);
    }
    finally
    {
        requestLog.Write(context);
    }
});

app.UseExceptionHandler();

if (!settings.SecureMode)
{
    app.UseCors();
}
else
{
    app.UseRateLimiter();
}

app.MapOpenApi();

app.MapGet("/", (AppSettings current) => Results.Ok(new
{
    name = "OefenApi",
    secureMode = current.SecureMode,
    openApi = "/openapi/v1.json"
}))
.WithName("Info")
.WithTags("Info")
.WithSummary("Laat zien of de API draait.");

RouteGroupBuilder api = app.MapGroup("/api");
RouteHandlerBuilder login = api.MapPost("/login", AuthEndpoints.Login)
    .WithName("Login")
    .WithTags("Auth")
    .WithSummary("Log in en ontvang een bearer token.")
    .Produces<LoginResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

if (settings.SecureMode)
{
    login.Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests);
    login.RequireRateLimiting("login");
}

RouteGroupBuilder secured = api.MapGroup("").AddEndpointFilter<BearerFilter>();
ClientEndpoints.Map(secured);
UserEndpoints.Map(secured, settings.SecureMode);
BulkEndpoints.Map(secured);
AdminEndpoints.Map(secured);

app.Run();
