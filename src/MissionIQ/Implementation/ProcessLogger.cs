using Microsoft.Extensions.Logging;
using MissionIQ.Abstractions;

namespace MissionIQ.Implementation;

/// <summary>
/// <see cref="ILogger"/>-backed implementation of <see cref="IProcessLogger"/>.
/// Each log entry is enriched with a structured <c>EventName</c> scope so that
/// Azure Monitor / Log Analytics can filter and query process activities.
/// </summary>
internal sealed class ProcessLogger : IProcessLogger
{
    private readonly ILogger _logger;

    public ProcessLogger(ILogger<ProcessLogger> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public void Log(
        LogLevel level,
        string eventName,
        string message,
        IDictionary<string, object?>? metadata = null,
        params object?[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        using (_logger.BeginScope(BuildScope(eventName, metadata)))
        {
#pragma warning disable CA2254 // Template should be a constant string: message is caller-controlled structured template
            _logger.Log(level, message, args);
#pragma warning restore CA2254
        }
    }

    /// <inheritdoc />
    public void LogProcessActivity(
        string activityName,
        string processId,
        LogLevel level = LogLevel.Information,
        IDictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);

        var enriched = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["process.id"] = processId,
            ["activity.name"] = activityName,
        };

        if (metadata is not null)
        {
            foreach (var kvp in metadata)
                enriched[kvp.Key] = kvp.Value;
        }

        using (_logger.BeginScope(BuildScope(activityName, enriched)))
        {
            _logger.Log(level, "Process activity '{ActivityName}' executed for process '{ProcessId}'",
                activityName, processId);
        }
    }

    private static Dictionary<string, object?> BuildScope(string eventName, IDictionary<string, object?>? metadata)
    {
        var scope = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["EventName"] = eventName,
        };

        if (metadata is not null)
        {
            foreach (var kvp in metadata)
                scope[kvp.Key] = kvp.Value;
        }

        return scope;
    }
}
