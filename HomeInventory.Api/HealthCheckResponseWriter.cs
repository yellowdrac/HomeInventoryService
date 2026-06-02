using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HomeInventory.Api;

/// <summary>
/// Serializes the health check result as JSON with the overall status,
/// database connection status, timestamp and assembly version.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var database = report.Entries.TryGetValue("postgres", out var entry)
            ? entry.Status.ToString()
            : "Unknown";

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

        var payload = new
        {
            status = report.Status.ToString(),
            database,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTimeOffset.UtcNow,
            version,
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
