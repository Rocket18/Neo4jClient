## Neo4jClient (Fork — Neo4j + Memgraph)
---

A .NET client for [Neo4j](https://neo4j.com) and [Memgraph](https://memgraph.com). Supports Cypher queries via fluent interfaces.

This is a fork of the original [Neo4jClient](https://github.com/DotNet4Neo4j/Neo4jClient) with the following changes:
- **Memgraph support** — temporal types and Bolt protocol compatibility for Memgraph
- **Newtonsoft.Json → System.Text.Json** — replaced all JSON serialization with `System.Text.Json`

Grab the latest drop straight from the `Neo4jClient` package on [NuGet](http://nuget.org/List/Packages/Neo4jClient).

---

## Key Changes in This Fork

### Memgraph Support

Memgraph uses the Bolt protocol and is largely compatible with the Neo4j Cypher API. This fork ensures correct handling of Memgraph-specific temporal type formats.

### .NET Type → Bolt Type Mapping

All date/time parameters are automatically converted to native Bolt temporal types, which is required by both Neo4j and Memgraph:

| .NET type | Bolt type | Memgraph / Neo4j display |
|---|---|---|
| `DateTimeOffset` | `ZonedDateTime` | `ZonedDateTime('2025-10-30T18:47:18+00:00[Etc/UTC]')` |
| `DateTime` | `LocalDateTime` | `LocalDateTime('2025-10-30T18:47:18')` |
| `DateOnly` *(NET6+)* | `LocalDate` | `Date('2025-10-30')` |

Example — no special handling needed, just assign your .NET types:

```csharp
await _client.GraphClient.Cypher
    .Create($"(g:{Neo4jNode.Movie})")
    .Set("g = $data")
    .WithParam("data", new { CreatedOn = DateTimeOffset.UtcNow, ReleaseDate = DateOnly.Today })
    .ExecuteWithoutResultsAsync();
```

### System.Text.Json Migration

`Newtonsoft.Json` has been fully replaced with `System.Text.Json`. Custom converters must now implement `System.Text.Json.Serialization.JsonConverter<T>` instead of `Newtonsoft.Json.JsonConverter`.

**Registering custom converters:**
```csharp
var client = new BoltGraphClient("bolt://localhost:7687", "neo4j", "password");
client.JsonConverters.Add(new MyCustomConverter());
await client.ConnectAsync();
```

**Ignoring properties:**

Both `[JsonIgnore]` (from `System.Text.Json`) and `[Neo4jIgnore]` are supported:
```csharp
public class Movie
{
    public string Title { get; set; }
    [JsonIgnore]
    public string InternalNote { get; set; }
    [Neo4jIgnore]
    public string AnotherIgnoredProp { get; set; }
}
```

**Custom property names:**
```csharp
public class Movie
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
}
```

**Passing native driver date types directly** — use `[Neo4jDateTime]` to bypass automatic conversion:
```csharp
public class Movie
{
    [Neo4jDateTime]
    public ZonedDateTime CreatedOn { get; set; }
}
```

---

## Connecting

### BoltGraphClient (Neo4j / Memgraph)

```csharp
// Neo4j
var client = new BoltGraphClient("neo4j://localhost:7687", "neo4j", "password");
await client.ConnectAsync();

// Memgraph
var client = new BoltGraphClient("bolt://localhost:7687", "memgraph", "password");
await client.ConnectAsync();
```

---

## Breaking Changes vs Original Neo4jClient

- **`Newtonsoft.Json` is no longer a dependency** — replace any `Newtonsoft.Json` converters or attributes
- **All endpoints are `async` only** — use `ExecuteWithoutResultsAsync()`, `ResultsAsync`, etc.
- **Date/time parameters are now sent as native Bolt types** — no longer serialized as ISO strings; this is required for correct storage in both Neo4j and Memgraph

---

## Original Package

You can find package: [Neo4jClient.Memgraph](https://www.nuget.org/packages/Neo4jClient.Memgraph)
The upstream project this is forked from: [DotNet4Neo4j/Neo4jClient](https://github.com/DotNet4Neo4j/Neo4jClient)

---

## License Information

Licensed under MS-PL. See `LICENSE` in the root of this repository for full license text.
