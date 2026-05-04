# 06-update-public-api: update-public-api

Update the public-facing serialization API surface: `IGraphClient`, `GraphClient`, `BoltGraphClient`, `CustomJsonSerializer`, `Execution/ExecutionConfiguration.cs`. Replace Newtonsoft types (`JsonConverter`, `DefaultContractResolver`) with their STJ equivalents (`JsonConverter` from STJ, `JsonSerializerOptions`).

Also update:
- `Transactions/ITransactionExecutionEnvironment.cs`
- `Transactions/TransactionExecutionEnvironment.cs`
- `HttpContentExtensions.cs`
- `StatementResultHelper.cs`
- `Neo4jDriverExtensions.cs`

**Done when**: No Newtonsoft types appear in any public interface or class signatures; all plumbing compiles; `IGraphClient` exposes STJ types only.
