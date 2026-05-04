# 02-swap-model-attributes: swap-model-attributes

Replace `[JsonProperty("name")]` / `[JsonProperty]` Newtonsoft attributes with `[JsonPropertyName("name")]` (from `System.Text.Json.Serialization`) on all API model classes and other POCO-style files. These files only use Newtonsoft for attributes and have no converter/DOM logic.

Affected files include: all `ApiModels/` files, `Cypher/CypherQuery.cs`, `Cypher/QueryWriter.cs`, `Cypher/CypherFluentQuery.cs`, `Cypher/CypherReturnExpressionBuilder.cs`, `Execution/ExecutionConfiguration.cs`, `Execution/HttpResponseMessageExtensions.cs`, `IndexConfiguration.cs`, `IndexMetaData.cs`, and any others using only `[JsonProperty]`.

**Done when**: All `[JsonProperty]` attributes from Newtonsoft are replaced with `[JsonPropertyName]` or `[JsonIgnore]` equivalents; no `Newtonsoft.Json` imports remain in any of these files; project builds.
