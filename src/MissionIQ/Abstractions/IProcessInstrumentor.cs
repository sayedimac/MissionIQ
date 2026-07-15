using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MissionIQ.Abstractions;

/// <summary>
/// Umbrella interface for business process observability.
/// Exposes traces, metrics, and logs as first-class concerns aligned with the
/// OpenTelemetry three-pillar model.
/// </summary>
public interface IProcessInstrumentor
{
    /// <summary>Gets the tracer for recording distributed trace spans.</summary>
    IProcessTracer Tracer { get; }

    /// <summary>Gets the metrics recorder for capturing point-in-time process measurements.</summary>
    IProcessMetrics Metrics { get; }

    /// <summary>Gets the logger for emitting structured process activity entries.</summary>
    IProcessLogger Logger { get; }

    /// <summary>
    /// Convenience method: starts a new trace activity.
    /// </summary>
    /// <param name="name">Name of the activity / process step.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>An <see cref="Activity"/> if sampling permits, otherwise <c>null</c>.</returns>
    Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Convenience method: increments a named counter metric.
    /// </summary>
    /// <param name="name">Metric counter name.</param>
    /// <param name="value">Delta to add; defaults to <c>1</c>.</param>
    /// <param name="tags">Optional metadata tags.</param>
    void AddCounter(string name, long value = 1, IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Convenience method: records a histogram measurement.
    /// </summary>
    /// <param name="name">Metric histogram name.</param>
    /// <param name="value">Measured value.</param>
    /// <param name="tags">Optional metadata tags.</param>
    void RecordHistogram(string name, double value, IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Convenience method: emits a structured log entry for a process activity.
    /// </summary>
    /// <param name="activityName">Name of the process activity step.</param>
    /// <param name="processId">Identifier of the business process instance.</param>
    /// <param name="level">Log severity level.</param>
    /// <param name="metadata">Optional key-value metadata.</param>
    void LogProcessActivity(
        string activityName,
        string processId,
        LogLevel level = LogLevel.Information,
        IDictionary<string, object?>? metadata = null);
}
