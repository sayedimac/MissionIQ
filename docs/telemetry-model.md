# Business process telemetry model

MissionIQ models one business process instance as one distributed trace. Keep trace and metric names stable; put instance-specific values in attributes.

## Signal model

| Business concept | OpenTelemetry signal | Workspace-based Application Insights table | Required dimensions |
|---|---|---|---|
| Process instance | Root `Server` span | `AppRequests` | `process.name`, `process.id` |
| Agent or activity | Child `Internal` span | `AppDependencies` | `activity.name`, `process.id`, `agent.name`; optional `agent.id` |
| Process KPI | Counter or histogram | `AppMetrics` | Bounded dimensions such as process name, activity name, status, and agent name |
| Activity record | Structured `ILogger` entry | `AppTraces` | `EventName`, `process.id`, `activity.name`, plus relevant metadata |

The exporter supplies `OperationId`, `ParentId`, and span identifiers. Do not copy trace IDs into custom properties. Logs emitted while an activity is current inherit trace correlation automatically.

## Instrumentation rules

- Use `StartProcess` once at the process boundary. Dispose it only when the complete process instance finishes.
- Use `StartAgentActivity` for each agent decision, tool call, handoff, or process step. Nested spans preserve causality.
- Set `ActivityStatusCode.Error` and a low-cardinality error category when work fails. Never attach prompts, credentials, access tokens, or sensitive payloads by default.
- Use counters for totals and histograms for distributions. Recommended names are exposed by `ProcessTelemetryConventions`.
- Never use `process.id`, `agent.id`, customer IDs, or other unbounded values as metric tags. Use them on spans and logs only.
- Use seconds as the unit for duration metrics. Record business outcomes as bounded values such as `succeeded`, `failed`, `cancelled`, or `timed_out`.

## Recommended metrics

| Name | Instrument | Suggested dimensions |
|---|---|---|
| `business.process.started` | Counter | `process.name` |
| `business.process.completed` | Counter | `process.name`, `activity.status` |
| `business.process.duration` | Histogram (seconds) | `process.name`, `activity.status` |
| `business.activity.completed` | Counter | `process.name`, `activity.name`, `agent.name`, `activity.status` |
| `business.activity.duration` | Histogram (seconds) | `process.name`, `activity.name`, `agent.name`, `activity.status` |

## Workspace-based LAW queries

End-to-end process traces:

```kusto
let processId = "process-123";
union
    (AppRequests | project TimeGenerated, ItemType="Process", Name, OperationId, ParentId, Id, DurationMs, Success, Properties),
    (AppDependencies | project TimeGenerated, ItemType="Activity", Name, OperationId, ParentId, Id, DurationMs, Success, Properties)
| where tostring(Properties["process.id"]) == processId
| order by TimeGenerated asc
```

Activity logs correlated to a process trace:

```kusto
AppTraces
| where TimeGenerated > ago(24h)
| where tostring(Properties["process.id"]) == "process-123"
| project TimeGenerated, OperationId, ParentId, SeverityLevel, Message,
          ActivityName=tostring(Properties["activity.name"]), Properties
| order by TimeGenerated asc
```

Process throughput and failures:

```kusto
AppMetrics
| where TimeGenerated > ago(7d)
| where Name == "business.process.completed"
| extend ProcessName=tostring(Properties["process.name"]),
         Status=tostring(Properties["activity.status"])
| summarize Total=sum(Sum) by ProcessName, Status, bin(TimeGenerated, 1h)
| order by TimeGenerated asc
```

Do not use classic Application Insights aliases such as `requests`, `dependencies`, `traces`, or `customMetrics` in LAW queries. Their workspace-based equivalents are `AppRequests`, `AppDependencies`, `AppTraces`, and `AppMetrics`.