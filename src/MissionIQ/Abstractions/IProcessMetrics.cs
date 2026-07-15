namespace MissionIQ.Abstractions;

/// <summary>
/// Provides metrics recording capabilities for business process observability.
/// Metrics capture point-in-time measurements of process indicators.
/// </summary>
public interface IProcessMetrics
{
    /// <summary>
    /// Increments a named counter by the given delta value.
    /// Use counters for cumulative measurements such as the number of orders processed.
    /// </summary>
    /// <param name="name">The name of the counter metric.</param>
    /// <param name="value">The amount to add to the counter. Defaults to <c>1</c>.</param>
    /// <param name="tags">Optional key-value metadata tags attached to the measurement.</param>
    void AddCounter(string name, long value = 1, IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Records a histogram measurement for a named metric.
    /// Use histograms for distributions such as processing duration or payload size.
    /// </summary>
    /// <param name="name">The name of the histogram metric.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="tags">Optional key-value metadata tags attached to the measurement.</param>
    void RecordHistogram(string name, double value, IEnumerable<KeyValuePair<string, object?>>? tags = null);

    /// <summary>
    /// Gets the name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by this metrics instance.
    /// </summary>
    string MeterName { get; }
}
