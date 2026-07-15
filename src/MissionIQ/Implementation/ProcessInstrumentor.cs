using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MissionIQ.Abstractions;

namespace MissionIQ.Implementation;

/// <summary>
/// Default implementation of <see cref="IProcessInstrumentor"/> that combines a
/// <see cref="IProcessTracer"/>, <see cref="IProcessMetrics"/> and <see cref="IProcessLogger"/>
/// into a single facade, making it easy to inject and use across business process components.
/// </summary>
internal sealed class ProcessInstrumentor : IProcessInstrumentor
{
    public ProcessInstrumentor(IProcessTracer tracer, IProcessMetrics metrics, IProcessLogger logger)
    {
        ArgumentNullException.ThrowIfNull(tracer);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);
        Tracer = tracer;
        Metrics = metrics;
        Logger = logger;
    }

    /// <inheritdoc />
    public IProcessTracer Tracer { get; }

    /// <inheritdoc />
    public IProcessMetrics Metrics { get; }

    /// <inheritdoc />
    public IProcessLogger Logger { get; }

    /// <inheritdoc />
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => Tracer.StartActivity(name, kind);

    /// <inheritdoc />
    public void AddCounter(string name, long value = 1, IEnumerable<KeyValuePair<string, object?>>? tags = null)
        => Metrics.AddCounter(name, value, tags);

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, IEnumerable<KeyValuePair<string, object?>>? tags = null)
        => Metrics.RecordHistogram(name, value, tags);

    /// <inheritdoc />
    public void LogProcessActivity(
        string activityName,
        string processId,
        LogLevel level = LogLevel.Information,
        IDictionary<string, object?>? metadata = null)
        => Logger.LogProcessActivity(activityName, processId, level, metadata);
}
