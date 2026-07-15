using System.Diagnostics.Metrics;
using MissionIQ.Abstractions;

namespace MissionIQ.Implementation;

/// <summary>
/// OpenTelemetry-backed implementation of <see cref="IProcessMetrics"/>.
/// </summary>
internal sealed class ProcessMetrics : IProcessMetrics, IDisposable
{
    private readonly Meter _meter;

    // Lazily created instruments, keyed by metric name.
    private readonly Dictionary<string, Counter<long>> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Histogram<double>> _histograms = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public ProcessMetrics(string meterName, string? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meterName);
        _meter = new Meter(meterName, serviceVersion);
    }

    /// <inheritdoc />
    public string MeterName => _meter.Name;

    /// <inheritdoc />
    public void AddCounter(string name, long value = 1, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var counter = GetOrCreateCounter(name);
        if (tags is not null)
            counter.Add(value, tags.ToArray());
        else
            counter.Add(value);
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var histogram = GetOrCreateHistogram(name);
        if (tags is not null)
            histogram.Record(value, tags.ToArray());
        else
            histogram.Record(value);
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        lock (_lock)
        {
            if (!_counters.TryGetValue(name, out var counter))
            {
                counter = _meter.CreateCounter<long>(name);
                _counters[name] = counter;
            }
            return counter;
        }
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        lock (_lock)
        {
            if (!_histograms.TryGetValue(name, out var histogram))
            {
                histogram = _meter.CreateHistogram<double>(name);
                _histograms[name] = histogram;
            }
            return histogram;
        }
    }

    public void Dispose() => _meter.Dispose();
}
