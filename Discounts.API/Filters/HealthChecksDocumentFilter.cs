using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Discounts.API.Filters;

/// <summary>
/// Injects the health-check endpoints into the Swagger document.
/// These endpoints are registered as middleware (not controllers), so
/// Swashbuckle cannot discover them automatically.
/// </summary>
internal sealed class HealthChecksDocumentFilter : IDocumentFilter
{
    private static readonly OpenApiTag HealthTag = new() { Name = "Health" };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Add("/health/live", BuildLivenessPath());
        swaggerDoc.Paths.Add("/health/ready", BuildReadinessPath());
    }

    private static OpenApiPathItem BuildLivenessPath() => new()
    {
        Operations =
        {
            [OperationType.Get] = new OpenApiOperation
            {
                Tags = [HealthTag],
                Summary = "Liveness probe",
                Description = "Returns **200 OK** as long as the API process is running. " +
                              "No health checks are executed — use `/health/ready` for a full dependency check.",
                OperationId = "Health_Live",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "Process is alive" },
                    ["503"] = new OpenApiResponse { Description = "Process is starting up or shutting down" }
                }
            }
        }
    };

    private static OpenApiPathItem BuildReadinessPath() => new()
    {
        Operations =
        {
            [OperationType.Get] = new OpenApiOperation
            {
                Tags = [HealthTag],
                Summary = "Readiness probe",
                Description = "Runs **all** registered health checks (SQL Server, EF Core, Worker Services) " +
                              "and returns a detailed JSON report. Returns **200** when every check passes, " +
                              "**503** when one or more checks are degraded or unhealthy.",
                OperationId = "Health_Ready",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "All checks passed",
                        Content = ReadinessContent()
                    },
                    ["503"] = new OpenApiResponse
                    {
                        Description = "One or more checks failed",
                        Content = ReadinessContent()
                    }
                }
            }
        }
    };

    /// <summary>
    /// Inline schema matching the JSON shape produced by
    /// <c>HealthChecks.UI.Client.UIResponseWriter</c>.
    /// </summary>
    private static Dictionary<string, OpenApiMediaType> ReadinessContent() => new()
    {
        ["application/json"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["status"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("Healthy") },
                    ["totalDuration"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("00:00:00.0123456") },
                    ["entries"] = new()
                    {
                        Type = "object",
                        AdditionalPropertiesAllowed = true,
                        AdditionalProperties = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["status"] = new() { Type = "string" },
                                ["duration"] = new() { Type = "string" },
                                ["description"] = new() { Type = "string", Nullable = true },
                                ["data"] = new() { Type = "object", AdditionalPropertiesAllowed = true }
                            }
                        }
                    }
                }
            }
        }
    };
}
