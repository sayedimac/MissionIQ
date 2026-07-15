using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MissionIQ.Abstractions;
using MissionIQ.Configuration;
using MissionIQ.Extensions;
using MissionIQ.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MissionIQ.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMissionIQ_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "TestService");

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IProcessInstrumentor>());
        Assert.NotNull(provider.GetService<IProcessTracer>());
        Assert.NotNull(provider.GetService<IProcessMetrics>());
        Assert.NotNull(provider.GetService<IProcessLogger>());
    }

    [Fact]
    public void AddMissionIQ_DefaultOptions_UsesDefaultServiceName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ();

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MissionIQOptions>>().Value;

        Assert.Equal("MissionIQ", opts.ServiceName);
    }

    [Fact]
    public void AddMissionIQ_ConfiguredOptions_AreReflected()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = "OrderService";
            o.ServiceVersion = "2.0";
            o.ServiceNamespace = "Ecommerce";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MissionIQOptions>>().Value;

        Assert.Equal("OrderService", opts.ServiceName);
        Assert.Equal("2.0", opts.ServiceVersion);
        Assert.Equal("Ecommerce", opts.ServiceNamespace);
    }

    [Fact]
    public void AddMissionIQ_Returns_Same_ServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var returned = services.AddMissionIQ();

        Assert.Same(services, returned);
    }

    [Fact]
    public void AddMissionIQ_NullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var ex = Record.Exception(() => services.AddMissionIQ(null));
        Assert.Null(ex);
    }

    [Fact]
    public void AddMissionIQ_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddMissionIQ());
    }
}

public class ProcessTracerTests
{
    [Fact]
    public void StartProcess_CreatesServerSpanWithProcessDimensions()
    {
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("ProcessSource")
            .Build()!;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "ProcessSource");

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();
        using var activity = tracer.StartProcess("OrderFulfillment", "process-123");

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
        Assert.Equal("OrderFulfillment", activity.GetTagItem(ProcessTelemetryConventions.ProcessName));
        Assert.Equal("process-123", activity.GetTagItem(ProcessTelemetryConventions.ProcessId));
    }

    [Fact]
    public void StartAgentActivity_CreatesInternalSpanWithAgentDimensions()
    {
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("AgentSource")
            .Build()!;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "AgentSource");

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();
        using var process = tracer.StartProcess("ClaimsReview", "claim-123");
        using var activity = tracer.StartAgentActivity("CheckPolicy", "claim-123", "PolicyAgent", "agent-7");

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(process!.TraceId, activity.TraceId);
        Assert.Equal(process.SpanId, activity.ParentSpanId);
        Assert.Equal("PolicyAgent", activity.GetTagItem(ProcessTelemetryConventions.AgentName));
        Assert.Equal("agent-7", activity.GetTagItem(ProcessTelemetryConventions.AgentId));
    }

    [Fact]
    public void StartActivity_ReturnsActivity_WhenListenerRegistered()
    {
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("TestSource")
            .AddInMemoryExporter(exportedActivities)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = "TestSource";
            o.ActivitySourceName = "TestSource";
        });

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();

        using var activity = tracer.StartActivity("TestOperation");

        Assert.NotNull(activity);
        Assert.Equal("TestOperation", activity.OperationName);
    }

    [Fact]
    public void ActivitySourceName_MatchesConfiguredName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = "MySvc";
            o.ActivitySourceName = "MyCustomSource";
        });

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();

        Assert.Equal("MyCustomSource", tracer.ActivitySourceName);
    }

    [Fact]
    public void ActivitySourceName_DefaultsToServiceName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "MySvc");

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();

        Assert.Equal("MySvc", tracer.ActivitySourceName);
    }

    [Fact]
    public void StartActivity_WithActivityKind_SetsKind()
    {
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("KindSource")
            .AddInMemoryExporter(exportedActivities)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = "KindSource";
            o.ActivitySourceName = "KindSource";
        });

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();

        using var activity = tracer.StartActivity("KindTest", ActivityKind.Producer);

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Producer, activity.Kind);
    }

    [Fact]
    public void StartActivity_EmptyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var tracer = provider.GetRequiredService<IProcessTracer>();

        Assert.Throws<ArgumentException>(() => tracer.StartActivity(string.Empty));
    }
}

public class ProcessMetricsTests
{
    [Fact]
    public void MeterName_MatchesConfiguredName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = "Svc";
            o.MeterName = "MyMeter";
        });

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        Assert.Equal("MyMeter", metrics.MeterName);
    }

    [Fact]
    public void MeterName_DefaultsToServiceName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "MySvc");

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        Assert.Equal("MySvc", metrics.MeterName);
    }

    [Fact]
    public void AddCounter_RecordsExpectedValue()
    {
        var exportedMetrics = new List<Metric>();
        var meterName = "MetricCounterTest";

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddInMemoryExporter(exportedMetrics)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = meterName;
            o.MeterName = meterName;
        });

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        metrics.AddCounter("orders.processed", 5);
        metrics.AddCounter("orders.processed", 3);

        meterProvider.ForceFlush();

        Assert.Contains(exportedMetrics, m => m.Name == "orders.processed");
    }

    [Fact]
    public void RecordHistogram_RecordsExpectedValue()
    {
        var exportedMetrics = new List<Metric>();
        var meterName = "MetricHistogramTest";

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddInMemoryExporter(exportedMetrics)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = meterName;
            o.MeterName = meterName;
        });

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        metrics.RecordHistogram("process.duration.ms", 120.5);

        meterProvider.ForceFlush();

        Assert.Contains(exportedMetrics, m => m.Name == "process.duration.ms");
    }

    [Fact]
    public void AddCounter_EmptyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        Assert.Throws<ArgumentException>(() => metrics.AddCounter(string.Empty));
    }

    [Fact]
    public void RecordHistogram_EmptyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<IProcessMetrics>();

        Assert.Throws<ArgumentException>(() => metrics.RecordHistogram(string.Empty, 1.0));
    }
}

public class ProcessLoggerTests
{
    [Fact]
    public void LogProcessActivity_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IProcessLogger>();

        var ex = Record.Exception(() =>
            logger.LogProcessActivity("OrderCreated", "proc-001"));

        Assert.Null(ex);
    }

    [Fact]
    public void LogProcessActivity_WithMetadata_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IProcessLogger>();

        var metadata = new Dictionary<string, object?>
        {
            ["order.id"] = "ORD-123",
            ["customer.id"] = "CUST-456",
        };

        var ex = Record.Exception(() =>
            logger.LogProcessActivity("OrderShipped", "proc-002", LogLevel.Information, metadata));

        Assert.Null(ex);
    }

    [Fact]
    public void Log_EmptyEventName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IProcessLogger>();

        Assert.Throws<ArgumentException>(() =>
            logger.Log(LogLevel.Information, string.Empty, "message"));
    }

    [Fact]
    public void LogProcessActivity_EmptyProcessId_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IProcessLogger>();

        Assert.Throws<ArgumentException>(() =>
            logger.LogProcessActivity("Activity", string.Empty));
    }
}

public class ProcessInstrumentorTests
{
    [Fact]
    public void Instrumentor_StartProcess_DelegatesToTracer()
    {
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("FacadeSource")
            .Build()!;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "FacadeSource");

        using var provider = services.BuildServiceProvider();
        var instrumentor = provider.GetRequiredService<IProcessInstrumentor>();
        using var activity = instrumentor.StartProcess("Payment", "payment-42");

        Assert.NotNull(activity);
        Assert.Equal("payment-42", activity.GetTagItem(ProcessTelemetryConventions.ProcessId));
    }

    [Fact]
    public void Instrumentor_ExposesAllPillars()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var instrumentor = provider.GetRequiredService<IProcessInstrumentor>();

        Assert.NotNull(instrumentor.Tracer);
        Assert.NotNull(instrumentor.Metrics);
        Assert.NotNull(instrumentor.Logger);
    }

    [Fact]
    public void Instrumentor_StartActivity_DelegatesToTracer()
    {
        var exportedActivities = new List<Activity>();
        var sourceName = "InstrumentorSource";

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(exportedActivities)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = sourceName;
            o.ActivitySourceName = sourceName;
        });

        using var provider = services.BuildServiceProvider();
        var instrumentor = provider.GetRequiredService<IProcessInstrumentor>();

        using var activity = instrumentor.StartActivity("CheckoutProcess");

        Assert.NotNull(activity);
        Assert.Equal("CheckoutProcess", activity.OperationName);
    }

    [Fact]
    public void Instrumentor_AddCounter_DelegatesToMetrics()
    {
        var exportedMetrics = new List<Metric>();
        var meterName = "InstrumentorMeter";

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddInMemoryExporter(exportedMetrics)
            .Build()!;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o =>
        {
            o.ServiceName = meterName;
            o.MeterName = meterName;
        });

        using var provider = services.BuildServiceProvider();
        var instrumentor = provider.GetRequiredService<IProcessInstrumentor>();

        instrumentor.AddCounter("payments.initiated", 1);
        meterProvider.ForceFlush();

        Assert.Contains(exportedMetrics, m => m.Name == "payments.initiated");
    }

    [Fact]
    public void Instrumentor_LogProcessActivity_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMissionIQ(o => o.ServiceName = "Svc");

        using var provider = services.BuildServiceProvider();
        var instrumentor = provider.GetRequiredService<IProcessInstrumentor>();

        var ex = Record.Exception(() =>
            instrumentor.LogProcessActivity("InvoiceGenerated", "proc-999"));

        Assert.Null(ex);
    }
}
