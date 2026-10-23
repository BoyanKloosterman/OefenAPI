using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using OefenApi.Models;

namespace OefenApi.Data;

internal sealed class RequestLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _directory;
    private readonly Lock _lock = new();

    public RequestLog(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    // Een regel per request. Geen wachtwoord, token of body.
    public void Write(HttpContext context)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        string? account = context.Items[RequestItems.Account] is Account current ? current.Username : null;
        string requestId = context.Items[RequestItems.RequestId] as string ?? "";

        var line = new RequestLogLine(
            now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            context.Request.Method,
            context.Request.Path.Value ?? "",
            context.Response.StatusCode,
            account,
            requestId,
            ReadTestRun(context));

        string fileName = "requests-" + now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl";
        string path = Path.Combine(_directory, fileName);
        string json = JsonSerializer.Serialize(line, JsonOptions);

        lock (_lock)
        {
            File.AppendAllText(path, json + "\n", Utf8);
        }
    }

    private static string? ReadTestRun(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Test-Run", out Microsoft.Extensions.Primitives.StringValues values))
        {
            return null;
        }

        string raw = values.ToString()
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "", StringComparison.Ordinal)
            .Trim();

        if (raw.Length == 0)
        {
            return null;
        }

        if (raw.Length > 80)
        {
            raw = raw[..80];
        }

        return raw;
    }

    private sealed record RequestLogLine(
        string Utc,
        string Local,
        string Method,
        string Path,
        int StatusCode,
        string? Account,
        string RequestId,
        string? TestRun);
}
