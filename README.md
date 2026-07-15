# MissionIQ

An extensible .NET package for instrumenting Business Processes using standard [OpenTelemetry](https://opentelemetry.io/) APIs with [Azure Monitor / Log Analytics](https://learn.microsoft.com/en-us/azure/azure-monitor/) Services.

MissionIQ exposes the three pillars of observability for business processes:

| Pillar | What it captures |
|--------|-----------------|
| **Traces** | How information flows between process systems and agents |
| **Metrics** | Point-in-time measurements of process indicators (counters, durations, etc.) |
| **Logs** | Structured activity records enriched with process context and metadata |

---

## Installation

```bash
dotnet add package MissionIQ
```


---

## Quick Start

### 1. Register MissionIQ with the DI container

```csharp
using MissionIQ.Extensions;

// In Program.cs / Startup.cs
builder.Services.AddMissionIQ(options =>
{
    options.ServiceName    = "OrderProcessing";
    options.ServiceVersion = "1.0.0";
    options.ServiceNamespace = "Ecommerce";

    // Export to Azure Monitor (Application Insights / Log Analytics)
    options.AzureMonitorConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

    // Optional: also emit to console (useful in development)
    options.EnableConsoleExporter = builder.Environment.IsDevelopment();
});
```

### 2. Inject and use `IProcessInstrumentor`

```csharp
using MissionIQ.Abstractions;

public class OrderProcessor
{
    private readonly IProcessInstrumentor _instrumentor;

    public OrderProcessor(IProcessInstrumentor instrumentor)
    {
        _instrumentor = instrumentor;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // ── Trace ──────────────────────────────────────────────────
        using var activity = _instrumentor.StartActivity("ProcessOrder");
        activity?.SetTag("order.id",    order.Id);
        activity?.SetTag("customer.id", order.CustomerId);

        // ── Metrics ────────────────────────────────────────────────
        _instrumentor.AddCounter("orders.received");

        var sw = Stopwatch.StartNew();
        try
        {
            await DoProcessingAsync(order);

            // ── Log ────────────────────────────────────────────────
            _instrumentor.LogProcessActivity(
                activityName: "OrderProcessed",
                processId:    order.ProcessId,
                metadata: new Dictionary<string, object?>
                {
                    ["order.id"]    = order.Id,
                    ["order.total"] = order.Total,
                });

            _instrumentor.AddCounter("orders.succeeded");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _instrumentor.AddCounter("orders.failed");

            _instrumentor.Logger.Log(
                LogLevel.Error, "OrderFailed",
                "Order {OrderId} failed: {Message}",
                metadata: null,
                order.Id, ex.Message);

            throw;
        }
        finally
        {
            sw.Stop();
            _instrumentor.RecordHistogram("order.processing.duration.ms", sw.Elapsed.TotalMilliseconds);
        }
    }
}
```

---

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServiceName` | `string` | `"MissionIQ"` | OpenTelemetry `service.name` resource attribute |
| `ServiceVersion` | `string?` | `null` | OpenTelemetry `service.version` attribute |
| `ServiceNamespace` | `string?` | `null` | OpenTelemetry `service.namespace` attribute |
| `AzureMonitorConnectionString` | `string?` | `null` | Azure Monitor connection string; enables all three signal exporters |
| `EnableConsoleExporter` | `bool` | `false` | Emit traces, metrics and logs to the console |
| `ActivitySourceName` | `string?` | `ServiceName` | Custom name for the `ActivitySource` (tracing) |
| `MeterName` | `string?` | `ServiceName` | Custom name for the `Meter` (metrics) |

---

## Core Abstractions

### `IProcessInstrumentor`

The primary façade. Inject this single interface to access all three observability pillars.

| Member | Description |
|--------|-------------|
| `Tracer` | Access to `IProcessTracer` |
| `Metrics` | Access to `IProcessMetrics` |
| `Logger` | Access to `IProcessLogger` |
| `StartActivity(name, kind)` | Start a new trace span |
| `AddCounter(name, value, tags)` | Increment a named counter |
| `RecordHistogram(name, value, tags)` | Record a histogram measurement |
| `LogProcessActivity(activityName, processId, level, metadata)` | Emit a structured process log entry |

### `IProcessTracer`

```csharp
Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);
```

### `IProcessMetrics`

```csharp
void AddCounter(string name, long value = 1, IEnumerable<KeyValuePair<string, object?>>? tags = null);
void RecordHistogram(string name, double value, IEnumerable<KeyValuePair<string, object?>>? tags = null);
```

### `IProcessLogger`

```csharp
void Log(LogLevel level, string eventName, string message,
         IDictionary<string, object?>? metadata = null, params object?[] args);

void LogProcessActivity(string activityName, string processId,
                        LogLevel level = LogLevel.Information,
                        IDictionary<string, object?>? metadata = null);
```

---

## Building and Testing

```bash
# Build
dotnet build

# Test
dotnet test
```

---

## License

[MIT](LICENSE)
