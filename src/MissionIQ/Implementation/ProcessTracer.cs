using System.Diagnostics;
using MissionIQ.Abstractions;
using MissionIQ.Telemetry;

namespace MissionIQ.Implementation;

/// <summary>
/// OpenTelemetry-backed implementation of <see cref="IProcessTracer"/>.
/// </summary>
internal sealed class ProcessTracer : IProcessTracer, IDisposable
{
    private readonly ActivitySource _activitySource;

    public ProcessTracer(string activitySourceName, string? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activitySourceName);
        _activitySource = new ActivitySource(activitySourceName, serviceVersion);
    }

    /// <inheritdoc />
    public string ActivitySourceName => _activitySource.Name;

    /// <inheritdoc />
    public Activity? StartProcess(
        string processName,
        string processId,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);

        var activityTags = BuildTags(tags,
            new(ProcessTelemetryConventions.ProcessName, processName),
            new(ProcessTelemetryConventions.ProcessId, processId));

        return _activitySource.StartActivity(processName, ActivityKind.Server, default(ActivityContext), activityTags);
    }

    /// <inheritdoc />
    public Activity? StartAgentActivity(
        string activityName,
        string processId,
        string agentName,
        string? agentId = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var requiredTags = new List<KeyValuePair<string, object?>>
        {
            new(ProcessTelemetryConventions.ActivityName, activityName),
            new(ProcessTelemetryConventions.ProcessId, processId),
            new(ProcessTelemetryConventions.AgentName, agentName),
        };

        if (!string.IsNullOrWhiteSpace(agentId))
            requiredTags.Add(new(ProcessTelemetryConventions.AgentId, agentId));

        return _activitySource.StartActivity(
            activityName,
            ActivityKind.Internal,
            default(ActivityContext),
            BuildTags(tags, requiredTags.ToArray()));
    }

    /// <inheritdoc />
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _activitySource.StartActivity(name, kind);
    }

    private static ActivityTagsCollection BuildTags(
        IEnumerable<KeyValuePair<string, object?>>? tags,
        params KeyValuePair<string, object?>[] requiredTags)
    {
        var values = tags is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(tags, StringComparer.Ordinal);

        foreach (var tag in requiredTags)
            values[tag.Key] = tag.Value;

        return new ActivityTagsCollection(values);
    }

    public void Dispose() => _activitySource.Dispose();
}
