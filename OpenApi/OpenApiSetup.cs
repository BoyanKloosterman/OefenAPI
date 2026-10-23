using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OefenApi.Settings;

namespace OefenApi.OpenApi;

internal static class OpenApiSetup
{
    public static void Add(IServiceCollection services, AppSettings settings)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "OefenApi",
                    Version = "v1",
                    Description = "Lokale oefen-API."
                };
                document.Servers =
                [
                    new OpenApiServer { Url = ListenAddress.Url }
                ];

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        In = ParameterLocation.Header,
                        BearerFormat = "opaque",
                        Description = "Token uit POST /api/login."
                    }
                };

                if (document.Paths is not null)
                {
                    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths)
                    {
                        if (IsAnonymous(path.Key) || path.Value.Operations is null)
                        {
                            continue;
                        }

                        foreach (OpenApiOperation operation in path.Value.Operations.Values)
                        {
                            operation.Security ??= [];
                            operation.Security.Add(new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                            });
                        }
                    }
                }

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (!settings.SecureMode || context.Description.RelativePath != "api/bulkdata")
                {
                    return Task.CompletedTask;
                }

                if (operation.Parameters is null)
                {
                    return Task.CompletedTask;
                }

                foreach (IOpenApiParameter parameter in operation.Parameters)
                {
                    if (!string.Equals(parameter.Name, "pageSize", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (parameter.Schema is OpenApiSchema schema)
                    {
                        schema.Minimum = "1";
                        schema.Maximum = "100";
                    }
                }

                return Task.CompletedTask;
            });
        });
    }

    private static bool IsAnonymous(string path)
    {
        return path.Equals("/", StringComparison.Ordinal)
            || path.Equals("/api/login", StringComparison.OrdinalIgnoreCase);
    }
}
