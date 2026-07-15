using System.Diagnostics;
using MissionIQ.Abstractions;

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
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _activitySource.StartActivity(name, kind);
    }

    public void Dispose() => _activitySource.Dispose();
}
