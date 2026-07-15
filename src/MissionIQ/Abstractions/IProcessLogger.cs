using Microsoft.Extensions.Logging;

namespace MissionIQ.Abstractions;

/// <summary>
/// Provides structured logging capabilities for business process observability.
/// Logs record each process activity with correct contextual metadata.
/// </summary>
public interface IProcessLogger
{
    /// <summary>
    /// Emits a structured log entry for a named business process activity.
    /// </summary>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="eventName">A short identifier for the type of process event being logged.</param>
    /// <param name="message">The human-readable log message, optionally using structured message templates.</param>
    /// <param name="metadata">Optional dictionary of additional key-value metadata to include with the entry.</param>
    /// <param name="args">Arguments to substitute into the message template.</param>
    void Log(
        LogLevel level,
        string eventName,
        string message,
        IDictionary<string, object?>? metadata = null,
        params object?[] args);

    /// <summary>
    /// Emits an <see cref="LogLevel.Information"/> log entry for a named business process activity,
    /// enriched with the process identifier and optional metadata.
    /// </summary>
    /// <param name="activityName">The name of the process activity step.</param>
    /// <param name="processId">The identifier of the business process instance.</param>
    /// <param name="level">The severity level of the log entry. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="metadata">Optional key-value metadata describing the activity.</param>
    void LogProcessActivity(
        string activityName,
        string processId,
        LogLevel level = LogLevel.Information,
        IDictionary<string, object?>? metadata = null);
}
