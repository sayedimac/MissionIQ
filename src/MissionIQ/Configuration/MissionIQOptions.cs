namespace MissionIQ.Configuration;

/// <summary>
/// Configuration options for MissionIQ process instrumentation.
/// </summary>
public sealed class MissionIQOptions
{
    /// <summary>
    /// The logical name of the service or process being instrumented.
    /// Used as the OpenTelemetry <c>service.name</c> resource attribute.
    /// Defaults to <c>"MissionIQ"</c>.
    /// </summary>
    public string ServiceName { get; set; } = "MissionIQ";

    /// <summary>
    /// Optional version string for the service, mapped to the <c>service.version</c>
    /// resource attribute.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Optional namespace for the service, mapped to the <c>service.namespace</c>
    /// resource attribute.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Azure Monitor (Application Insights) connection string.
    /// When set, traces, metrics and logs are exported to Azure Log Analytics.
    /// </summary>
    public string? AzureMonitorConnectionString { get; set; }

    /// <summary>
    /// When <c>true</c>, a console exporter is added for all three telemetry signals.
    /// Useful for local development and debugging.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// The name used to create the <see cref="System.Diagnostics.ActivitySource"/> for tracing.
    /// Defaults to the value of <see cref="ServiceName"/>.
    /// </summary>
    public string? ActivitySourceName { get; set; }

    /// <summary>
    /// The name used to create the <see cref="System.Diagnostics.Metrics.Meter"/> for metrics.
    /// Defaults to the value of <see cref="ServiceName"/>.
    /// </summary>
    public string? MeterName { get; set; }

    internal string ResolvedActivitySourceName => ActivitySourceName ?? ServiceName;
    internal string ResolvedMeterName => MeterName ?? ServiceName;
}
