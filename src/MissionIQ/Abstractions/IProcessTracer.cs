using System.Diagnostics;

namespace MissionIQ.Abstractions;

/// <summary>
/// Provides tracing capabilities for business process observability.
/// Traces capture how information flows between the various process systems and agents.
/// </summary>
public interface IProcessTracer
{
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
