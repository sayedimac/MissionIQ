namespace MissionIQ.Telemetry;

/// <summary>
/// Stable attribute and metric names used by MissionIQ telemetry.
/// </summary>
public static class ProcessTelemetryConventions
{
    public const string ProcessId = "process.id";
    public const string ProcessName = "process.name";
    public const string AgentId = "agent.id";
    public const string AgentName = "agent.name";
    public const string ActivityName = "activity.name";
    public const string ActivityStatus = "activity.status";

    public const string ProcessesStarted = "business.process.started";
    public const string ProcessesCompleted = "business.process.completed";
    public const string ProcessDuration = "business.process.duration";
    public const string ActivitiesCompleted = "business.activity.completed";
    public const string ActivityDuration = "business.activity.duration";
}