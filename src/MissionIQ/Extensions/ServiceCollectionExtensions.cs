using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MissionIQ.Abstractions;
using MissionIQ.Configuration;
using MissionIQ.Implementation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MissionIQ.Extensions;

/// <summary>
/// Extension methods to register MissionIQ process instrumentation with the .NET DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MissionIQ's process instrumentation services and configures
    /// the OpenTelemetry SDK for traces, metrics, and logs.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A callback to configure <see cref="MissionIQOptions"/>.</param>
    /// <returns>The original <paramref name="services"/> to allow chaining.</returns>
    public static IServiceCollection AddMissionIQ(
        this IServiceCollection services,
        Action<MissionIQOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MissionIQOptions();
        configure?.Invoke(options);

        // Register options as a singleton so implementations can read configuration.
        services.AddSingleton(Options.Create(options));

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceNamespace: options.ServiceNamespace);

        // ── Tracing ───────────────────────────────────────────────────────────────
        var otelBuilder = services.AddOpenTelemetry();

        otelBuilder.WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(resourceBuilder)
                .AddSource(options.ResolvedActivitySourceName);

            if (options.EnableConsoleExporter)
                tracing.AddConsoleExporter();

            if (!string.IsNullOrWhiteSpace(options.AzureMonitorConnectionString))
                tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = options.AzureMonitorConnectionString);
        });

        // ── Metrics ───────────────────────────────────────────────────────────────
        otelBuilder.WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(resourceBuilder)
                .AddMeter(options.ResolvedMeterName);

            if (options.EnableConsoleExporter)
                metrics.AddConsoleExporter();

            if (!string.IsNullOrWhiteSpace(options.AzureMonitorConnectionString))
                metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = options.AzureMonitorConnectionString);
        });

        // ── Logging ───────────────────────────────────────────────────────────────
        otelBuilder.WithLogging(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);

            if (options.EnableConsoleExporter)
                logging.AddConsoleExporter();

            if (!string.IsNullOrWhiteSpace(options.AzureMonitorConnectionString))
                logging.AddAzureMonitorLogExporter(o => o.ConnectionString = options.AzureMonitorConnectionString);
        });

        // ── Instrumentation services ──────────────────────────────────────────────
        services.AddSingleton<IProcessTracer>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MissionIQOptions>>().Value;
            return new ProcessTracer(opts.ResolvedActivitySourceName, opts.ServiceVersion);
        });

        services.AddSingleton<IProcessMetrics>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MissionIQOptions>>().Value;
            return new ProcessMetrics(opts.ResolvedMeterName, opts.ServiceVersion);
        });

        services.AddSingleton<IProcessLogger>(sp =>
            new ProcessLogger(sp.GetRequiredService<ILogger<ProcessLogger>>()));

        services.AddSingleton<IProcessInstrumentor, ProcessInstrumentor>();

        return services;
    }
}
