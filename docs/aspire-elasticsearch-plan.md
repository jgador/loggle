# Aspire Dashboard with Elasticsearch Persistence - Implementation Plan

## Executive Summary

This document provides a detailed plan for implementing persistent storage of telemetry data (logs, traces, and metrics) from the .NET Aspire Dashboard using Elasticsearch. The Aspire Dashboard currently stores all telemetry data in-memory, which means data is lost when the dashboard restarts. This plan outlines how to integrate Elasticsearch as a persistent backend while maintaining the dashboard's real-time capabilities.

## Table of Contents

1. [Overview of Aspire Dashboard](#overview-of-aspire-dashboard)
2. [Current Architecture Analysis](#current-architecture-analysis)
3. [Why Elasticsearch for Persistence](#why-elasticsearch-for-persistence)
4. [Proposed Architecture](#proposed-architecture)
5. [Implementation Steps](#implementation-steps)
6. [Configuration](#configuration)
7. [Testing Strategy](#testing-strategy)
8. [Deployment Considerations](#deployment-considerations)
9. [Alternative Approaches](#alternative-approaches)

## Overview of Aspire Dashboard

### What is Aspire Dashboard?

The Aspire Dashboard is a browser-based application for viewing run-time information about distributed applications built with .NET Aspire. It provides:

- **Resource monitoring**: View all resources (services, containers, databases) in your application
- **Live console logs**: Real-time console output from each resource
- **Structured logs**: View logs with full context and filtering capabilities
- **Distributed tracing**: Visualize request flows across services
- **Metrics**: Monitor performance metrics in real-time

### Key Technical Details

**Location**: `aspire/src/Aspire.Dashboard/`

**Technology Stack**:
- ASP.NET Core Blazor (for UI)
- gRPC (for OTLP telemetry ingestion)
- OpenTelemetry Protocol (OTLP) for telemetry collection
- In-memory storage (current limitation)

**OTLP Endpoints**:
- gRPC endpoint: `http://localhost:18889` (default)
- HTTP endpoint: `http://localhost:18890` (default)

## Current Architecture Analysis

### Data Flow

```
Application → OTLP Exporter → Aspire Dashboard OTLP Service → TelemetryRepository (In-Memory) → Blazor UI
```

### In-Memory Storage Implementation

The dashboard uses `TelemetryRepository` class located at:
`aspire/src/Aspire.Dashboard/Otlp/Storage/TelemetryRepository.cs`

**Key Components**:

1. **TelemetryRepository**: Central storage for all telemetry
   - Uses `CircularBuffer<T>` for logs and traces
   - `ConcurrentDictionary` for resources
   - `ReaderWriterLockSlim` for thread-safe access

2. **Storage Limits** (configurable via `Dashboard:TelemetryLimits`):
   - `MaxLogCount`: 10,000 logs per resource (default)
   - `MaxTraceCount`: 10,000 traces (default)
   - `MaxMetricsCount`: 50,000 metric data points (default)

3. **Data Structures**:
   - `OtlpLogEntry`: Structured log entries
   - `OtlpTrace`: Distributed trace data
   - `OtlpResource`: Resource metadata
   - Circular buffers automatically discard oldest data when limit is reached

### Current Limitations

1. **Data Loss on Restart**: All telemetry data is lost when dashboard restarts
2. **Limited History**: Only stores most recent N entries (configurable)
3. **No Long-term Analysis**: Cannot analyze trends over time
4. **No Multi-instance Support**: Each dashboard instance has its own data
5. **Memory Constraints**: Large applications may hit memory limits

## Why Elasticsearch for Persistence

### Benefits

1. **Purpose-built for Logs and Metrics**: Elasticsearch excels at storing and querying time-series data
2. **Powerful Search**: Full-text search and complex queries on log data
3. **Scalability**: Can handle massive volumes of telemetry data
4. **Data Retention**: Keep historical data for compliance and analysis
5. **Existing Ecosystem**:
   - Kibana for advanced visualization
   - Elastic APM for additional APM features
   - Large community and tooling support
6. **Integration with Loggle**: The loggle repository already uses Elasticsearch, making this a natural fit

### Synergy with Loggle

The loggle repository is a self-hosted log monitoring solution that uses:
- Elasticsearch for storage
- Kibana for visualization
- OpenTelemetry Collector for ingestion

Integrating Aspire Dashboard with Elasticsearch creates a unified observability platform where:
- Aspire Dashboard provides real-time, application-centric views
- Elasticsearch provides persistent storage
- Kibana provides long-term analysis and custom dashboards

## Proposed Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        .NET Applications                              │
│              (Services, APIs, Background Workers)                     │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ OTLP (gRPC/HTTP)
                            ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    Aspire Dashboard                                   │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  OTLP Service (Logs, Traces, Metrics)                       │    │
│  └──────────────────────┬─────────────────────────────────────┘    │
│                         │                                            │
│  ┌──────────────────────┴─────────────────────────────────────┐    │
│  │  Dual-Storage Layer                                         │    │
│  │  ┌─────────────────┐         ┌──────────────────────┐     │    │
│  │  │  In-Memory      │         │  Elasticsearch       │     │    │
│  │  │  Cache          │◄───────►│  Persistence         │     │    │
│  │  │  (CircularBuffer)│        │  (Long-term Storage) │     │    │
│  │  └─────────────────┘         └──────────────────────┘     │    │
│  └────────────────────┬─────────────────────────────────────┘    │
│                       │                                            │
│  ┌────────────────────┴─────────────────────────────────────┐    │
│  │  Blazor UI (Resources, Logs, Traces, Metrics)            │    │
│  └──────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                            │
                            ↓
                  ┌───────────────────┐
                  │   Elasticsearch   │
                  │     Cluster       │
                  └───────────────────┘
                            │
                            ↓
                  ┌───────────────────┐
                  │     Kibana        │
                  │  (Advanced Views) │
                  └───────────────────┘
```

### Component Architecture

#### 1. Storage Abstraction Layer

Create an abstraction to support multiple storage backends:

**Interface**: `ITelemetryStorage`

```csharp
public interface ITelemetryStorage
{
    // Logs
    Task StoreLogs(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken);
    Task<PagedResult<OtlpLogEntry>> GetLogs(GetLogsRequest request, CancellationToken cancellationToken);

    // Traces
    Task StoreTraces(IEnumerable<OtlpTrace> traces, CancellationToken cancellationToken);
    Task<PagedResult<OtlpTrace>> GetTraces(GetTracesRequest request, CancellationToken cancellationToken);

    // Metrics
    Task StoreMetrics(IEnumerable<OtlpMetric> metrics, CancellationToken cancellationToken);
    Task<PagedResult<OtlpMetric>> GetMetrics(GetMetricsRequest request, CancellationToken cancellationToken);

    // Resources
    Task StoreResource(OtlpResource resource, CancellationToken cancellationToken);
    Task<IEnumerable<OtlpResource>> GetResources(CancellationToken cancellationToken);
}
```

#### 2. Hybrid Storage Implementation

Implement a hybrid approach combining in-memory and Elasticsearch storage:

**Class**: `HybridTelemetryStorage`

**Strategy**:
- **Write Path**:
  - Synchronously write to in-memory cache (existing behavior)
  - Asynchronously write to Elasticsearch in background
  - Use batching and buffering for efficiency

- **Read Path**:
  - First check in-memory cache (for recent data)
  - Fall back to Elasticsearch for older data
  - Merge results if needed

**Benefits**:
- Maintains existing low-latency read performance
- Preserves real-time dashboard updates
- Adds persistence without breaking existing functionality
- Graceful degradation if Elasticsearch is unavailable

#### 3. Elasticsearch Integration

**Client Library**: `Elastic.Clients.Elasticsearch` (latest official client)

**Index Strategy**:

```
aspire-logs-{date}         # e.g., aspire-logs-2026-03-14
aspire-traces-{date}       # e.g., aspire-traces-2026-03-14
aspire-metrics-{date}      # e.g., aspire-metrics-2026-03-14
aspire-resources           # Single index for resources
```

**Index Templates**:
- Define mappings for efficient storage and querying
- Configure appropriate analyzers for text fields
- Set up index lifecycle management (ILM) policies

#### 4. Background Writer Service

**Class**: `ElasticsearchWriterService`

**Responsibilities**:
- Batch telemetry data for efficient writes
- Handle connection failures and retries
- Monitor write queue depth
- Report health metrics

**Implementation Pattern**: `BackgroundService` with Channel-based queue

```csharp
public class ElasticsearchWriterService : BackgroundService
{
    private readonly Channel<TelemetryBatch> _writeChannel;
    private readonly ElasticClient _client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in _writeChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await WriteBatchAsync(batch, stoppingToken);
        }
    }
}
```

## Implementation Steps

### Phase 1: Foundation (Week 1)

#### Step 1.1: Set Up Development Environment

```bash
# Clone Aspire repository (already done)
cd /tmp/aspire

# Verify Elasticsearch is running (use loggle's Elasticsearch)
cd /home/runner/work/loggle/loggle/examples
./loggle-compose.ps1 start

# Verify Elasticsearch is accessible
curl http://localhost:9200/_cluster/health
```

#### Step 1.2: Create Storage Abstraction

**Location**: `aspire/src/Aspire.Dashboard/Otlp/Storage/`

**Files to Create**:

1. `ITelemetryStorage.cs` - Storage interface
2. `InMemoryTelemetryStorage.cs` - Wrapper for existing implementation
3. `ElasticsearchTelemetryStorage.cs` - Elasticsearch implementation
4. `HybridTelemetryStorage.cs` - Combined implementation

**Refactoring Required**:
- Extract storage operations from `TelemetryRepository.cs`
- Keep the repository as a coordinator
- Move actual storage to abstraction implementations

#### Step 1.3: Add Elasticsearch Dependencies

**File**: `aspire/src/Aspire.Dashboard/Aspire.Dashboard.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Elastic.Clients.Elasticsearch" Version="8.x.x" />
  <PackageReference Include="Elastic.Transport" Version="0.x.x" />
</ItemGroup>
```

### Phase 2: Elasticsearch Integration (Week 2)

#### Step 2.1: Create Elasticsearch Client Factory

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/ElasticsearchClientFactory.cs`

```csharp
public class ElasticsearchClientFactory
{
    public ElasticClient CreateClient(ElasticsearchOptions options)
    {
        var settings = new ElasticsearchClientSettings(new Uri(options.Url))
            .DefaultMappingFor<OtlpLogEntry>(m => m.IndexName("aspire-logs"))
            .DefaultMappingFor<OtlpTrace>(m => m.IndexName("aspire-traces"))
            .DefaultMappingFor<OtlpMetric>(m => m.IndexName("aspire-metrics"));

        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            settings.Authentication(new ApiKey(options.ApiKey));
        }

        return new ElasticClient(settings);
    }
}
```

#### Step 2.2: Define Index Mappings

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/ElasticsearchMappings.cs`

Create index templates for each data type with optimized mappings:

```csharp
public static class ElasticsearchMappings
{
    public static async Task CreateIndexTemplates(ElasticClient client)
    {
        await CreateLogIndexTemplate(client);
        await CreateTraceIndexTemplate(client);
        await CreateMetricIndexTemplate(client);
    }

    private static async Task CreateLogIndexTemplate(ElasticClient client)
    {
        // Define mapping for log entries
        var template = new PutIndexTemplateRequest("aspire-logs-template")
        {
            IndexPatterns = new[] { "aspire-logs-*" },
            Template = new Template
            {
                Mappings = new TypeMapping
                {
                    Properties = new Properties
                    {
                        { "timestamp", new DateProperty() },
                        { "traceId", new KeywordProperty() },
                        { "spanId", new KeywordProperty() },
                        { "severity", new KeywordProperty() },
                        { "body", new TextProperty { Analyzer = "standard" } },
                        { "resource", new ObjectProperty { /* ... */ } },
                        { "attributes", new ObjectProperty { /* ... */ } }
                    }
                }
            }
        };

        await client.Indices.PutTemplateAsync(template);
    }
}
```

#### Step 2.3: Implement Elasticsearch Storage

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/ElasticsearchTelemetryStorage.cs`

Key methods:

```csharp
public async Task StoreLogs(IEnumerable<OtlpLogEntry> logs, CancellationToken ct)
{
    var indexName = $"aspire-logs-{DateTime.UtcNow:yyyy-MM-dd}";

    var bulkRequest = new BulkRequest(indexName)
    {
        Operations = logs.Select(log => new BulkIndexOperation<OtlpLogEntry>(log)
        {
            Id = log.Id
        }).ToList<IBulkOperation>()
    };

    var response = await _client.BulkAsync(bulkRequest, ct);

    if (!response.IsValidResponse)
    {
        _logger.LogError("Failed to index logs: {Error}", response.DebugInformation);
    }
}

public async Task<PagedResult<OtlpLogEntry>> GetLogs(GetLogsRequest request, CancellationToken ct)
{
    var searchRequest = new SearchRequest("aspire-logs-*")
    {
        From = request.Offset,
        Size = request.Limit,
        Query = BuildLogQuery(request),
        Sort = new[] { new SortField("timestamp", new FieldSort { Order = SortOrder.Desc }) }
    };

    var response = await _client.SearchAsync<OtlpLogEntry>(searchRequest, ct);

    return new PagedResult<OtlpLogEntry>
    {
        Items = response.Documents.ToList(),
        TotalCount = response.Total
    };
}
```

#### Step 2.4: Implement Background Writer

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/ElasticsearchWriterService.cs`

```csharp
public class ElasticsearchWriterService : BackgroundService
{
    private readonly Channel<TelemetryBatch> _channel;
    private readonly IElasticsearchTelemetryStorage _storage;
    private readonly ILogger<ElasticsearchWriterService> _logger;

    // Buffer for batching writes
    private const int BatchSize = 100;
    private const int MaxBatchWaitMs = 1000;

    public ElasticsearchWriterService(
        IElasticsearchTelemetryStorage storage,
        ILogger<ElasticsearchWriterService> logger)
    {
        _storage = storage;
        _logger = logger;
        _channel = Channel.CreateUnbounded<TelemetryBatch>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task EnqueueBatch(TelemetryBatch batch)
    {
        await _channel.Writer.WriteAsync(batch);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elasticsearch writer service started");

        await foreach (var batch in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessBatch(batch, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry batch");
                // Consider retry logic here
            }
        }
    }

    private async Task ProcessBatch(TelemetryBatch batch, CancellationToken ct)
    {
        if (batch.Logs?.Any() == true)
        {
            await _storage.StoreLogs(batch.Logs, ct);
        }

        if (batch.Traces?.Any() == true)
        {
            await _storage.StoreTraces(batch.Traces, ct);
        }

        if (batch.Metrics?.Any() == true)
        {
            await _storage.StoreMetrics(batch.Metrics, ct);
        }
    }
}
```

### Phase 3: Hybrid Storage Implementation (Week 3)

#### Step 3.1: Implement Hybrid Storage

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/HybridTelemetryStorage.cs`

```csharp
public class HybridTelemetryStorage : ITelemetryStorage
{
    private readonly InMemoryTelemetryStorage _memoryStorage;
    private readonly ElasticsearchTelemetryStorage _elasticsearchStorage;
    private readonly ElasticsearchWriterService _writerService;
    private readonly DashboardOptions _options;

    public async Task StoreLogs(IEnumerable<OtlpLogEntry> logs, CancellationToken ct)
    {
        // Synchronous write to in-memory (maintains real-time performance)
        await _memoryStorage.StoreLogs(logs, ct);

        // Asynchronous write to Elasticsearch (if enabled)
        if (_options.Elasticsearch?.Enabled == true)
        {
            await _writerService.EnqueueBatch(new TelemetryBatch { Logs = logs });
        }
    }

    public async Task<PagedResult<OtlpLogEntry>> GetLogs(GetLogsRequest request, CancellationToken ct)
    {
        // For recent data, use in-memory cache
        if (request.TimeRange.IsRecent(_options.InMemoryRetentionMinutes))
        {
            return await _memoryStorage.GetLogs(request, ct);
        }

        // For older data, query Elasticsearch
        if (_options.Elasticsearch?.Enabled == true)
        {
            try
            {
                return await _elasticsearchStorage.GetLogs(request, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query Elasticsearch, falling back to in-memory");
            }
        }

        // Fallback to in-memory if Elasticsearch is unavailable
        return await _memoryStorage.GetLogs(request, ct);
    }
}
```

#### Step 3.2: Modify TelemetryRepository

**File**: `aspire/src/Aspire.Dashboard/Otlp/Storage/TelemetryRepository.cs`

Refactor to use the storage abstraction:

```csharp
public sealed partial class TelemetryRepository : IDisposable
{
    private readonly ITelemetryStorage _storage;

    public TelemetryRepository(
        ITelemetryStorage storage,  // Injected abstraction
        ILoggerFactory loggerFactory,
        IOptions<DashboardOptions> dashboardOptions,
        PauseManager pauseManager,
        IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        _storage = storage;
        // ... rest of initialization
    }

    public async Task AddLogs(AddContext context, RepeatedField<ResourceLogs> resourceLogs)
    {
        var logs = ParseLogs(resourceLogs);
        await _storage.StoreLogs(logs, context.CancellationToken);

        // Trigger subscriptions for real-time updates
        await NotifyLogSubscribers(logs);
    }
}
```

### Phase 4: Configuration and Registration (Week 3)

#### Step 4.1: Add Configuration Options

**File**: `aspire/src/Aspire.Dashboard/Configuration/DashboardOptions.cs`

```csharp
public class DashboardOptions
{
    // Existing options...

    public ElasticsearchOptions? Elasticsearch { get; set; }
    public int InMemoryRetentionMinutes { get; set; } = 60; // Keep 1 hour in memory
}

public class ElasticsearchOptions
{
    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "http://localhost:9200";
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string IndexPrefix { get; set; } = "aspire";
    public int BatchSize { get; set; } = 100;
    public int BatchDelayMs { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
}
```

#### Step 4.2: Register Services

**File**: `aspire/src/Aspire.Dashboard/DashboardWebApplication.cs`

```csharp
public static async Task Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // ... existing configuration

    // Register storage services
    var elasticsearchOptions = builder.Configuration
        .GetSection("Dashboard:Elasticsearch")
        .Get<ElasticsearchOptions>();

    if (elasticsearchOptions?.Enabled == true)
    {
        // Register Elasticsearch services
        builder.Services.AddSingleton<ElasticsearchClientFactory>();
        builder.Services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<ElasticsearchClientFactory>();
            return factory.CreateClient(elasticsearchOptions);
        });

        builder.Services.AddSingleton<ElasticsearchTelemetryStorage>();
        builder.Services.AddHostedService<ElasticsearchWriterService>();

        // Register hybrid storage
        builder.Services.AddSingleton<ITelemetryStorage, HybridTelemetryStorage>();
    }
    else
    {
        // Use in-memory only
        builder.Services.AddSingleton<ITelemetryStorage, InMemoryTelemetryStorage>();
    }

    // ... rest of application setup
}
```

#### Step 4.3: Add Configuration File Example

**File**: `aspire/src/Aspire.Dashboard/appsettings.json`

```json
{
  "Dashboard": {
    "TelemetryLimits": {
      "MaxLogCount": 10000,
      "MaxTraceCount": 10000,
      "MaxMetricsCount": 50000
    },
    "Elasticsearch": {
      "Enabled": true,
      "Url": "http://localhost:9200",
      "IndexPrefix": "aspire",
      "BatchSize": 100,
      "BatchDelayMs": 1000,
      "MaxRetries": 3
    },
    "InMemoryRetentionMinutes": 60
  }
}
```

### Phase 5: Testing (Week 4)

#### Step 5.1: Unit Tests

Create unit tests for:

1. **ElasticsearchTelemetryStorage**
   - Test CRUD operations
   - Test error handling
   - Test connection failures
   - Use Testcontainers for real Elasticsearch instance

2. **HybridTelemetryStorage**
   - Test read/write path routing
   - Test fallback behavior
   - Test data consistency

3. **ElasticsearchWriterService**
   - Test batching logic
   - Test retry behavior
   - Test graceful shutdown

**Location**: `aspire/tests/Aspire.Dashboard.Tests/Otlp/Storage/`

**Example Test**:

```csharp
[Fact]
public async Task StoreLogs_Should_IndexToElasticsearch()
{
    // Arrange
    await using var container = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.11.0")
        .Build();

    await container.StartAsync();

    var options = new ElasticsearchOptions
    {
        Url = container.GetConnectionString()
    };

    var factory = new ElasticsearchClientFactory();
    var client = factory.CreateClient(options);
    var storage = new ElasticsearchTelemetryStorage(client, logger);

    var logs = new[]
    {
        new OtlpLogEntry
        {
            TimeUnixNano = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds(),
            Severity = LogLevel.Information,
            Body = "Test log entry"
        }
    };

    // Act
    await storage.StoreLogs(logs, CancellationToken.None);
    await Task.Delay(1000); // Wait for indexing

    // Assert
    var searchResponse = await client.SearchAsync<OtlpLogEntry>(s => s
        .Index("aspire-logs-*")
        .Query(q => q.MatchAll())
    );

    Assert.True(searchResponse.IsValidResponse);
    Assert.Single(searchResponse.Documents);
}
```

#### Step 5.2: Integration Tests

Create integration tests that:

1. Start Aspire Dashboard with Elasticsearch enabled
2. Send OTLP data via gRPC
3. Verify data appears in Elasticsearch
4. Verify data can be queried from UI

**Location**: `aspire/tests/Aspire.Dashboard.Tests/Integration/`

#### Step 5.3: Performance Tests

Measure:
- Write throughput with Elasticsearch enabled
- Query latency for hybrid reads
- Memory usage comparison
- Background writer queue depth under load

### Phase 6: Documentation (Week 4)

#### Step 6.1: Update Dashboard README

**File**: `aspire/src/Aspire.Dashboard/README.md`

Add section on Elasticsearch persistence:

```markdown
### Elasticsearch Persistence

By default, the Aspire Dashboard stores telemetry data in-memory. To enable persistent storage with Elasticsearch:

1. Configure Elasticsearch connection:
   ```json
   {
     "Dashboard": {
       "Elasticsearch": {
         "Enabled": true,
         "Url": "http://localhost:9200"
       }
     }
   }
   ```

2. (Optional) Configure authentication:
   ```json
   {
     "Dashboard": {
       "Elasticsearch": {
         "Enabled": true,
         "Url": "https://my-cluster.elastic-cloud.com",
         "ApiKey": "your-api-key"
       }
     }
   }
   ```

The dashboard uses a hybrid storage approach:
- Recent data (last hour) is served from in-memory cache for low latency
- Older data is automatically queried from Elasticsearch
- All data is persisted to Elasticsearch in the background
```

#### Step 6.2: Create Migration Guide

**File**: `aspire/docs/elasticsearch-persistence.md`

Document:
- Architecture overview
- Configuration options
- Index management
- Backup and restore procedures
- Troubleshooting

## Configuration

### Environment Variables

```bash
# Elasticsearch connection
Dashboard__Elasticsearch__Enabled=true
Dashboard__Elasticsearch__Url=http://localhost:9200
Dashboard__Elasticsearch__ApiKey=your-api-key

# Or use basic auth
Dashboard__Elasticsearch__Username=elastic
Dashboard__Elasticsearch__Password=changeme

# Performance tuning
Dashboard__Elasticsearch__BatchSize=100
Dashboard__Elasticsearch__BatchDelayMs=1000
Dashboard__InMemoryRetentionMinutes=60
```

### JSON Configuration

```json
{
  "Dashboard": {
    "Elasticsearch": {
      "Enabled": true,
      "Url": "http://localhost:9200",
      "ApiKey": "your-api-key",
      "IndexPrefix": "aspire",
      "BatchSize": 100,
      "BatchDelayMs": 1000,
      "MaxRetries": 3
    },
    "InMemoryRetentionMinutes": 60,
    "TelemetryLimits": {
      "MaxLogCount": 10000,
      "MaxTraceCount": 10000,
      "MaxMetricsCount": 50000
    }
  }
}
```

### Index Lifecycle Management

Configure ILM policies for automatic index management:

```json
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {
          "rollover": {
            "max_size": "50GB",
            "max_age": "1d"
          }
        }
      },
      "warm": {
        "min_age": "7d",
        "actions": {
          "shrink": {
            "number_of_shards": 1
          },
          "forcemerge": {
            "max_num_segments": 1
          }
        }
      },
      "delete": {
        "min_age": "30d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}
```

## Testing Strategy

### Local Testing Setup

1. **Start Elasticsearch** (using loggle's docker-compose):
   ```bash
   cd /home/runner/work/loggle/loggle/examples
   ./loggle-compose.ps1 start
   ```

2. **Configure Aspire Dashboard**:
   ```bash
   export Dashboard__Elasticsearch__Enabled=true
   export Dashboard__Elasticsearch__Url=http://localhost:9200
   ```

3. **Run Aspire Dashboard**:
   ```bash
   cd /tmp/aspire/src/Aspire.Dashboard
   dotnet run
   ```

4. **Run Sample Application**:
   ```bash
   cd /tmp/aspire/playground/TestShop
   dotnet run
   ```

5. **Verify Data in Elasticsearch**:
   ```bash
   # Check indices
   curl http://localhost:9200/_cat/indices?v

   # Query logs
   curl http://localhost:9200/aspire-logs-*/_search?pretty

   # View in Kibana
   open http://localhost:5601
   ```

### Test Scenarios

1. **Basic Functionality**:
   - Verify logs appear in dashboard UI
   - Verify logs are persisted to Elasticsearch
   - Verify traces are persisted
   - Verify metrics are persisted

2. **Failure Scenarios**:
   - Stop Elasticsearch during operation
   - Verify dashboard continues to work (in-memory only)
   - Restart Elasticsearch
   - Verify writes resume automatically

3. **Performance Testing**:
   - Generate high volume of telemetry
   - Monitor write queue depth
   - Monitor query latency
   - Monitor memory usage

4. **Data Retention**:
   - Verify old data is queryable from Elasticsearch
   - Verify in-memory cache expires correctly
   - Verify seamless transition between cache and persistent storage

## Deployment Considerations

### Production Deployment

#### 1. Elasticsearch Cluster Setup

**Recommended Configuration**:
- Minimum 3 nodes for high availability
- Dedicated master nodes for large deployments
- Hot-warm-cold architecture for cost optimization
- Snapshot and restore configured

**Resource Requirements**:
- Memory: 8GB+ per node (50% for heap)
- Disk: SSD recommended for hot data
- Network: Low latency between nodes

#### 2. Security

**Authentication**:
- Use API keys for application access
- Rotate keys regularly
- Use separate keys for read/write access

**Network Security**:
- TLS for all connections
- Network segmentation
- Firewall rules

**Encryption**:
- Enable TLS for transport and HTTP
- Consider encryption at rest

#### 3. Monitoring

**Metrics to Monitor**:
- Write queue depth
- Index rate
- Query latency
- Disk usage
- Memory usage
- Elasticsearch cluster health

**Alerting**:
- Alert on write failures
- Alert on high queue depth
- Alert on query errors
- Alert on Elasticsearch cluster issues

#### 4. Backup and Recovery

**Backup Strategy**:
- Configure snapshot repository
- Schedule regular snapshots
- Test restore procedures
- Document recovery procedures

**Example Snapshot Configuration**:
```bash
# Register snapshot repository
PUT _snapshot/aspire_backup
{
  "type": "fs",
  "settings": {
    "location": "/mnt/backups/elasticsearch",
    "compress": true
  }
}

# Create snapshot policy
PUT _slm/policy/daily-snapshots
{
  "schedule": "0 1 * * *",
  "name": "<daily-snap-{now/d}>",
  "repository": "aspire_backup",
  "config": {
    "indices": ["aspire-*"],
    "include_global_state": false
  },
  "retention": {
    "expire_after": "30d",
    "min_count": 5,
    "max_count": 50
  }
}
```

### Kubernetes Deployment

**Elasticsearch Deployment**:
```yaml
# Use Elastic Cloud on Kubernetes (ECK) operator
apiVersion: elasticsearch.k8s.elastic.co/v1
kind: Elasticsearch
metadata:
  name: aspire-es
spec:
  version: 8.11.0
  nodeSets:
  - name: default
    count: 3
    config:
      node.store.allow_mmap: false
    volumeClaimTemplates:
    - metadata:
        name: elasticsearch-data
      spec:
        accessModes:
        - ReadWriteOnce
        resources:
          requests:
            storage: 100Gi
        storageClassName: fast-ssd
```

**Aspire Dashboard Deployment**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aspire-dashboard
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: dashboard
        image: mcr.microsoft.com/dotnet/aspire-dashboard:8.0
        env:
        - name: Dashboard__Elasticsearch__Enabled
          value: "true"
        - name: Dashboard__Elasticsearch__Url
          value: "http://aspire-es-http:9200"
        - name: Dashboard__Elasticsearch__ApiKey
          valueFrom:
            secretKeyRef:
              name: elasticsearch-credentials
              key: api-key
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
```

### Azure Deployment

**Option 1: Azure Elasticsearch Service** (when available)
- Fully managed
- Integrated with Azure Monitor
- Auto-scaling

**Option 2: Elastic Cloud on Azure**
- Official Elastic Cloud offering
- Managed by Elastic
- Native Azure integration

**Option 3: Self-hosted on AKS**
- Use ECK operator on AKS
- Full control
- More operational overhead

### Integration with Loggle

Since loggle already uses Elasticsearch:

1. **Shared Elasticsearch Cluster**:
   - Use the same Elasticsearch instance
   - Use different index prefixes (`aspire-*` vs `logs-*`)
   - Share infrastructure costs

2. **Unified Kibana Dashboards**:
   - Create Kibana dashboards combining both data sources
   - Correlate application metrics with infrastructure logs
   - Single pane of glass for observability

3. **Configuration Example**:
   ```json
   {
     "Dashboard": {
       "Elasticsearch": {
         "Enabled": true,
         "Url": "http://elasticsearch:9200",
         "ApiKey": "${ELASTICSEARCH_API_KEY}",
         "IndexPrefix": "aspire"
       }
     }
   }
   ```

## Alternative Approaches

### Alternative 1: OpenTelemetry Collector with Elasticsearch Exporter

Instead of modifying the dashboard, use OTel Collector:

```
Application → Aspire Dashboard (in-memory only)
Application → OTel Collector → Elasticsearch Exporter → Elasticsearch
```

**Pros**:
- No dashboard modifications needed
- Standard OpenTelemetry approach
- Collector handles batching and retries

**Cons**:
- Dashboard cannot query Elasticsearch
- Two separate data stores
- More infrastructure to manage

**When to use**: If you only need persistence but don't need the dashboard to query historical data.

### Alternative 2: Sidecar Pattern

Deploy Elasticsearch writer as a sidecar:

```
Aspire Dashboard → gRPC → Elasticsearch Writer Sidecar → Elasticsearch
```

**Pros**:
- Minimal dashboard modifications
- Separate concerns
- Can be developed independently

**Cons**:
- More complex deployment
- gRPC communication overhead
- Requires protocol buffers

### Alternative 3: Database Abstraction with Multiple Backends

Support multiple backends (PostgreSQL, MongoDB, Elasticsearch):

```csharp
public interface ITelemetryStorage { }

public class ElasticsearchStorage : ITelemetryStorage { }
public class PostgresStorage : ITelemetryStorage { }
public class MongoStorage : ITelemetryStorage { }
```

**Pros**:
- Flexibility in backend choice
- Can choose based on requirements

**Cons**:
- Much more development effort
- Each backend has different capabilities
- Harder to optimize for each backend

### Alternative 4: Existing APM Solutions

Use existing APM solutions instead:

- **Elastic APM**: Full APM solution from Elastic
- **Jaeger**: Distributed tracing system
- **Zipkin**: Distributed tracing system
- **Application Insights**: Azure's APM solution

**Pros**:
- Production-ready
- Feature-rich
- Well-documented

**Cons**:
- May not integrate with Aspire Dashboard
- Different UX
- May have licensing costs

## Recommended Approach

For this project, I recommend **Alternative 1 (Hybrid Storage Implementation)** because:

1. **Maintains Dashboard Experience**: Users continue to use the Aspire Dashboard UI they're familiar with
2. **Real-time + Historical**: Combines low-latency real-time data with long-term persistence
3. **Graceful Degradation**: Works even if Elasticsearch is unavailable
4. **Synergy with Loggle**: Integrates well with existing Loggle infrastructure
5. **Incremental Adoption**: Can be rolled out gradually without breaking changes

## Summary

This plan provides a comprehensive approach to adding Elasticsearch persistence to the Aspire Dashboard while maintaining its real-time capabilities. The hybrid storage approach ensures:

- **Zero downtime**: Dashboard continues to work if Elasticsearch fails
- **Low latency**: Recent data served from in-memory cache
- **Complete history**: All data persisted for long-term analysis
- **Easy integration**: Works with existing Loggle infrastructure

The implementation can be done in phases over approximately 4 weeks, with each phase building on the previous one. The result is a production-ready observability platform that combines the best of both worlds: Aspire's excellent real-time dashboard with Elasticsearch's powerful persistence and search capabilities.

## Next Steps

1. **Review and Approve Plan**: Stakeholders review this document
2. **Set Up Development Environment**: Clone repos, start services
3. **Phase 1 Implementation**: Create storage abstractions
4. **Iterative Development**: Implement phases 2-6
5. **Testing and Validation**: Comprehensive testing
6. **Documentation**: Complete all documentation
7. **Production Deployment**: Deploy to production environment

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)
- [Elasticsearch Documentation](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)
- [Elastic .NET Client](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)
- [Loggle Repository](https://github.com/jgador/loggle)

---

**Document Version**: 1.0
**Date**: 2026-03-14
**Author**: AI Assistant
**Status**: Draft for Review
