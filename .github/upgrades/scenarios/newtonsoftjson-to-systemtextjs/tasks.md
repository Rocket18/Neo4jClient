# Newtonsoft.Json → System.Text.Json Progress

## Overview

Migrating `Neo4jClient` from Newtonsoft.Json to System.Text.Json across 1 library project and 1 test project. The migration covers custom converters, the JToken-based DOM deserializer, the contract resolver, and the public API surface. This is a breaking change requiring a major version bump.

**Progress**: 7/9 tasks complete (78%) ![78%](https://progress-bar.xyz/78)

## Tasks

- ✅ 01-update-package-reference: Remove Newtonsoft.Json, add System.Text.Json package reference
- ✅ 02-swap-model-attributes: Replace [JsonProperty] with [JsonPropertyName] on model/POCO files
- ✅ 03-rewrite-custom-converters: Rewrite 5 custom converters for System.Text.Json
- ✅ 04-replace-jtokens-dom: Replace JToken/JArray/JValue DOM with JsonNode/JsonElement
- ✅ 05-rewrite-contract-resolver: Replace DefaultContractResolver with STJ equivalent
- ✅ 06-update-public-api: Update IGraphClient and all public API to use STJ types
- ✅ 07-update-serializer-core: Rewrite CustomJsonSerializer internals with JsonSerializerOptions
- 🔄 08-update-tests: Update test files to use STJ types
- 🔲 09-final-validation: Full build, test run, remaining reference check, migration report
