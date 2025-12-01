using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds service defaults to the host application builder.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Adds OpenAPI documentation with JWT Bearer security scheme.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="title">The API title.</param>
    /// <param name="version">The API version (default: v1).</param>
    /// <param name="description">Optional API description.</param>
    public static IHostApplicationBuilder AddDefaultOpenApi(
        this IHostApplicationBuilder builder,
        string title,
        string version = "v1",
        string? description = null)
    {
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = title,
                    Version = version,
                    Description = description,
                    Contact = new OpenApiContact
                    {
                        Name = "InnoClinic Team"
                    }
                };
                return Task.CompletedTask;
            });

            // Add JWT Bearer security scheme
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Enter your JWT token obtained from Keycloak"
                    }
                };
                return Task.CompletedTask;
            });

            // Apply security requirement to all operations
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                // Check if the endpoint has [AllowAnonymous] attribute
                var allowAnonymous = context.Description.ActionDescriptor.EndpointMetadata
                    .Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);

                if (!allowAnonymous)
                {
                    var schemeReference = new OpenApiSecuritySchemeReference("Bearer");
                    
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [schemeReference] = []
                    });
                }

                return Task.CompletedTask;
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for the application.
    /// </summary>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Adds default health checks to the application.
    /// </summary>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps default health check endpoints for Aspire dashboard integration.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        // Ready check for all checks tagged with "ready"
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });

        return app;
    }

    /// <summary>
    /// Maps OpenAPI JSON endpoint and Scalar UI for API documentation.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="developmentOnly">If true, only enables in development environment (default: false).</param>
    public static WebApplication MapDefaultOpenApi(this WebApplication app, bool developmentOnly = false)
    {
        if (developmentOnly && !app.Environment.IsDevelopment())
        {
            return app;
        }

        // Map the OpenAPI JSON endpoint
        app.MapOpenApi();

        // Map Scalar UI at /scalar
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle($"{app.Environment.ApplicationName} API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .WithPreferredScheme("Bearer");
        });

        return app;
    }
}
