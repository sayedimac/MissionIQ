using System.Diagnostics;

namespace MissionIQ.Abstractions;

/// <summary>
/// Provides tracing capabilities for business process observability.
/// Traces capture how information flows between the various process systems and agents.
/// </summary>
public interface IProcessTracer
{
    /// <summary>
    /// Starts the root server span for one business process instance.
    /// In workspace-based Application Insights this span is queryable in <c>AppRequests</c>.
    /// </summary>
    /// <param name="processName">Stable process definition name.</param>
    /// <param name="processId">Identifier of this process instance.</param>
    /// <param name="tags">Optional process metadata. Avoid secrets and high-cardinality values other than identifiers needed for correlation.</param>
    Activity? StartProcess(
        string processName,
        string processId,
        IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Starts a child span for work performed by an agent within a process.
    /// In workspace-based Application Insights this span is queryable in <c>AppDependencies</c>.
    /// </summary>
    /// <param name="activityName">Stable activity or process-step name.</param>
    /// <param name="processId">Identifier of the containing process instance.</param>
    /// <param name="agentName">Stable name of the responsible agent or component.</param>
    /// <param name="agentId">Optional identifier of the agent instance.</param>
    /// <param name="tags">Optional activity metadata.</param>
    Activity? StartAgentActivity(
        string activityName,
        string processId,
        string agentName,
        string? agentId = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Starts a new activity (trace span) representing a unit of work in a business process.
    /// </summary>
    /// <param name="name">The name of the activity, typically reflecting the process step or operation.</param>
    /// <param name="kind">The activity kind indicating the role of the span in the trace.</param>
    /// <returns>An <see cref="Activity"/> if sampling allows, otherwise <c>null</c>.</returns>
    Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Gets the name of the <see cref="ActivitySource"/> used by this tracer.
    /// </summary>
    string ActivitySourceName { get; }
}
