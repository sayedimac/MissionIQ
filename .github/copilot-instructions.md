# MissionIQ project guidelines

## Architecture

- MissionIQ is a .NET 8 library built on OpenTelemetry APIs and the Azure Monitor exporter.
- Preserve the public abstractions in `src/MissionIQ/Abstractions`; prefer additive, backward-compatible API changes.
- Follow `docs/telemetry-model.md` whenever changing instrumentation, telemetry dimensions, documentation, or queries.

## Telemetry conventions

- Model one business process instance as a trace: a root `Server` span created by `StartProcess`, with nested `Internal` spans created by `StartAgentActivity`.
- Use constants from `ProcessTelemetryConventions`; do not duplicate telemetry attribute or metric-name string literals in library code.
- Workspace-based Application Insights uses `AppRequests` for process roots, `AppDependencies` for child spans, `AppMetrics` for metrics, and `AppTraces` for logs.
- In LAW KQL, use `TimeGenerated`, `Properties`, `OperationId`, and `ParentId`. Do not introduce classic aliases such as `requests`, `dependencies`, `customMetrics`, `customDimensions`, or `operation_Id`.
- Keep metric dimensions bounded. Never add process IDs, agent instance IDs, customer IDs, prompts, payloads, secrets, or tokens as metric tags.
- Put correlation identifiers on spans and structured logs. Rely on OpenTelemetry for trace/span correlation rather than copying IDs into custom properties.
- Emit structured logs while the relevant activity is current so Azure Monitor correlates them through `OperationId`.

## Code and validation

- Keep nullable reference types enabled and follow the existing C# style and XML documentation conventions.
- Add or update xUnit tests for public behavior and telemetry attributes.
- Run `dotnet test MissionIQ.slnx --configuration Release` after changes.
- Keep README examples and `docs/telemetry-model.md` synchronized with public APIs and LAW schema mappings.