using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Neo4jClient.ApiModels;
using Neo4jClient.Cypher;

namespace Neo4jClient.Serialization
{
    public class CypherJsonDeserializer<TResult> : ICypherJsonDeserializer<TResult>
    {
        readonly IGraphClient client;
        readonly CypherResultMode resultMode;
        private readonly CypherResultFormat resultFormat;
        private readonly bool inTransaction;
        private readonly bool inBolt;

        readonly CultureInfo culture = CultureInfo.InvariantCulture;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CypherJsonDeserializer() { }

        public CypherJsonDeserializer(IGraphClient client, CypherResultMode resultMode, CypherResultFormat resultFormat)
            : this(client, resultMode, resultFormat, false, false)
        {
        }

        public CypherJsonDeserializer(IGraphClient client, CypherResultMode resultMode, CypherResultFormat resultFormat, bool inTransaction)
            : this(client, resultMode, resultFormat, inTransaction, false)
        {
        }

        public CypherJsonDeserializer(IGraphClient client, CypherResultMode resultMode, CypherResultFormat resultFormat, bool inTransaction, bool inBolt)
        {
            this.client = client;
            this.resultMode = resultMode;
            this.inTransaction = inTransaction;
            this.inBolt = inBolt;
            // here is where we decide if we should deserialize as transactional or REST endpoint data format.
            if (resultFormat == CypherResultFormat.DependsOnEnvironment)
            {
                this.resultFormat = inTransaction ? CypherResultFormat.Transactional : CypherResultFormat.Rest;
            }
            else
            {
                this.resultFormat = resultFormat;
            }
        }

        public IEnumerable<TResult> Deserialize(string content, bool isHttp)
        {
            try
            {
                var context = new DeserializationContext
                {
                    Culture = culture,
                    JsonConverters = Enumerable.Reverse(client.JsonConverters ?? new List<System.Text.Json.Serialization.JsonConverter>(0)).ToArray(),
                    JsonSerializerOptions = client.JsonSerializerOptions
                };

                if(isHttp) content = CommonDeserializerMethods.RemoveResultsFromJson(content);
                content = CommonDeserializerMethods.ReplaceAllDateInstancesWithNeoDates(content);

                var jsonOptions = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

                // Force the deserialization to happen now, not later, as there's
                // not much value to deferred execution here and we'd like to know
                // about any errors now
                return inTransaction
                    ? FullDeserializationFromTransactionResponse(content, context, isHttp).ToArray()
                    : DeserializeFromRoot(content, context, isHttp).ToArray();
            }
            catch (Exception ex)
            {
                // we want the NeoException to be thrown
                if (ex is NeoException)
                {
                    throw;
                }

                const string messageTemplate =
                    @"Neo4j returned a valid response, however Neo4jClient was unable to deserialize into the object structure you supplied.

First, try and review the exception below to work out what broke.

If it's not obvious, you can ask for help at http://stackoverflow.com/questions/tagged/neo4jclient

Include the full text of this exception, including this message, the stack trace, and all of the inner exception details.

Include the full type definition of {0}.

Include this raw JSON, with any sensitive values replaced with non-sensitive equivalents:

{1}";
                var message = string.Format(messageTemplate, typeof(TResult).FullName, content);

                // If it's a specifc scenario that we're blowing up about, put this front and centre in the message
                if (ex is DeserializationException deserializationException)
                {
                    message = $"{deserializationException.Message}{Environment.NewLine}{Environment.NewLine}----{Environment.NewLine}{Environment.NewLine}{message}";
                }

                throw new ArgumentException(message, nameof(content), ex);
            }
        }

        private IEnumerable<TResult> DeserializeInternal(string content, bool isHttp)
        {
            var context = new DeserializationContext
            {
                Culture = culture,
                JsonConverters = Enumerable.Reverse(client.JsonConverters ?? new List<System.Text.Json.Serialization.JsonConverter>(0)).ToArray(),
                JsonSerializerOptions = client.JsonSerializerOptions
            };
            content = CommonDeserializerMethods.ReplaceAllDateInstancesWithNeoDates(content);

            var root = JsonNode.Parse(content);

            var columnsArray = root["columns"] as JsonArray;
            var columnNames = columnsArray
                .Select(c => c.AsString())
                .ToArray();

            var jsonTypeMappings = new List<TypeMapping>
            {
                new TypeMapping
                {
                    ShouldTriggerForPropertyType = (nestingLevel, type) =>
                        type.GetTypeInfo().IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(Node<>),
                    DetermineTypeToParseJsonIntoBasedOnPropertyType = t =>
                    {
                        var nodeType = t.GetGenericArguments();
                        return typeof (NodeApiResponse<>).MakeGenericType(nodeType);
                    },
                    MutationCallback = n => n.GetType().GetMethod("ToNode").Invoke(n, new object[] { client })
                },
                new TypeMapping
                {
                    ShouldTriggerForPropertyType = (nestingLevel, type) =>
                        type.GetTypeInfo().IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(RelationshipInstance<>),
                    DetermineTypeToParseJsonIntoBasedOnPropertyType = t =>
                    {
                        var relationshipType = t.GetGenericArguments();
                        return typeof (RelationshipApiResponse<>).MakeGenericType(relationshipType);
                    },
                    MutationCallback = n => n.GetType().GetMethod("ToRelationshipInstance").Invoke(n, new object[] { client })
                }
            };

            switch (resultMode)
            {
                case CypherResultMode.Set: return ParseInSingleColumnMode(context, root, columnNames, jsonTypeMappings.ToArray(), isHttp);
                case CypherResultMode.Projection:
                    jsonTypeMappings.Add(new TypeMapping
                    {
                        ShouldTriggerForPropertyType = (nestingLevel, type) => nestingLevel == 0 && type.GetTypeInfo().IsClass,
                        DetermineTypeToParseJsonIntoBasedOnPropertyType = t => typeof(NodeOrRelationshipApiResponse<>).MakeGenericType(new[] { t }),
                        MutationCallback = n => n.GetType().GetProperty("Data").GetGetMethod().Invoke(n, new object[0])
                    });
                    return ParseInProjectionMode(context, root, columnNames, jsonTypeMappings.ToArray(), isHttp);
                default:
                    throw new NotSupportedException($"Unrecognised result mode of {resultMode}.");
            }
        }

        private IEnumerable<TResult> DeserializeResultSet(JsonNode resultRoot, DeserializationContext context, bool isHttp)
        {
            var columnsArray = isHttp ? resultRoot["results"]?[0]?["columns"] as JsonArray : resultRoot["columns"] as JsonArray;
            if (columnsArray == null)
                columnsArray = !isHttp ? resultRoot["results"]?[0]?["columns"] as JsonArray : resultRoot["columns"] as JsonArray;
            var columnNames = columnsArray
                .Select(c => c.AsString())
                .ToArray();

            var jsonTypeMappings = new List<TypeMapping>
            {
                new TypeMapping
                {
                    ShouldTriggerForPropertyType = (nestingLevel, type) =>
                        type.GetTypeInfo().IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(Node<>),
                    DetermineTypeToParseJsonIntoBasedOnPropertyType = t =>
                    {
                        var nodeType = t.GetGenericArguments();
                        return typeof (NodeApiResponse<>).MakeGenericType(nodeType);
                    },
                    MutationCallback = n => n.GetType().GetMethod("ToNode").Invoke(n, new object[] { client })
                },
                new TypeMapping
                {
                    ShouldTriggerForPropertyType = (nestingLevel, type) =>
                        type.GetTypeInfo().IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(RelationshipInstance<>),
                    DetermineTypeToParseJsonIntoBasedOnPropertyType = t =>
                    {
                        var relationshipType = t.GetGenericArguments();
                        return typeof (RelationshipApiResponse<>).MakeGenericType(relationshipType);
                    },
                    MutationCallback = n => n.GetType().GetMethod("ToRelationshipInstance").Invoke(n, new object[] { client })
                }
            };

            switch (resultMode)
            {
                case CypherResultMode.Set:
                    return ParseInSingleColumnMode(context, resultRoot, columnNames, jsonTypeMappings.ToArray(), isHttp);
                case CypherResultMode.Projection:
                    // if we are in transaction and we have an object we dont need a mutation
                    if (!inTransaction && !inBolt)
                    {
                        // jsonTypeMappings.Add(new TypeMapping
                        // {
                        //     ShouldTriggerForPropertyType = (nestingLevel, type) =>
                        //         nestingLevel == 0 && type.GetTypeInfo().IsClass,
                        //     DetermineTypeToParseJsonIntoBasedOnPropertyType = t =>
                        //         typeof(NodeOrRelationshipApiResponse<>).MakeGenericType(new[] { t }),
                        //     MutationCallback = n =>
                        //         n.GetType().GetProperty("Data").GetGetMethod().Invoke(n, new object[0])
                        // });
                    }
                    return ParseInProjectionMode(context, resultRoot, columnNames, jsonTypeMappings.ToArray(), isHttp);
                default:
                    throw new NotSupportedException($"Unrecognised result mode of {resultMode}.");
            }
        }

        private string GetStringPropertyFromObject(JsonObject obj, string propertyName)
        {
            if (obj.ContainsKey(propertyName))
                return obj[propertyName]?.GetValue<string>();
            return null;
        }

        private NeoException BuildNeoException(JsonNode error)
        {
            var errorObject = error as JsonObject;
            var code = GetStringPropertyFromObject(errorObject, "code");
            if (code == null)
                throw new InvalidOperationException("Expected 'code' property on error message");

            var message = GetStringPropertyFromObject(errorObject, "message");
            if (message == null)
                throw new InvalidOperationException("Expected 'message' property on error message");

            var lastCodePart = code.Substring(code.LastIndexOf('.') + 1);
            return new NeoException(new ExceptionResponse
            {
                StackTrace = new string[] { },
                Exception = lastCodePart,
                FullName = code,
                Message = message
            });
        }

        public PartialDeserializationContext CheckForErrorsInTransactionResponse(string content)
        {
            if (!inTransaction)
                throw new InvalidOperationException("Deserialization of this type must be done inside of a transaction scope.");

            var context = new DeserializationContext
            {
                Culture = culture,
                JsonConverters = Enumerable.Reverse(client.JsonConverters ?? new List<System.Text.Json.Serialization.JsonConverter>(0)).ToArray()
            };
            content = CommonDeserializerMethods.ReplaceAllDateInstancesWithNeoDates(content);

            var root = JsonNode.Parse(content) as JsonObject;

            return new PartialDeserializationContext
            {
                RootResult = GetRootResultInTransaction(root),
                DeserializationContext = context
            };
        }

        private JsonNode GetRootResultInTransaction(JsonObject root)
        {
            if (root == null)
                throw new InvalidOperationException("Root expected to be a JSON object.");

            var rawErrors = root["errors"] as JsonArray;
            if (rawErrors != null && rawErrors.Count > 0)
                throw BuildNeoException(rawErrors[0]);

            var rawResults = root["results"] as JsonArray;
            if (rawResults == null)
                throw new InvalidOperationException("Expected `results` property on JSON root object");

            return rawResults.Count > 0 ? rawResults[0] : null;
        }

        public IEnumerable<TResult> DeserializeFromTransactionPartialContext(PartialDeserializationContext context, bool isHttp)
        {
            if (context.RootResult == null)
            {
                throw new InvalidOperationException(
                    @"`results` array should have one result set.
This means no query was emitted, so a method that doesn't care about getting results should have been called."
                    );
            }
            return DeserializeResultSet(context.RootResult, context.DeserializationContext, isHttp);
        }

        private IEnumerable<TResult> FullDeserializationFromTransactionResponse(string content, DeserializationContext context, bool isHttp)
        {
            content = CommonDeserializerMethods.NormalizeSingleQuotedJson(content);
            var root = JsonNode.Parse(content) as JsonObject;
            var resultSet = GetRootResultInTransaction(root);
            if (resultSet == null)
                throw new InvalidOperationException(
                    @"`results` array should have one result set.
This means no query was emitted, so a method that doesn't care about getting results should have been called."
                    );
            return DeserializeResultSet(resultSet, context, isHttp);
        }

        IEnumerable<TResult> DeserializeFromRoot(string content, DeserializationContext context, bool isHttp)
        {
            content = CommonDeserializerMethods.NormalizeSingleQuotedJson(content);
            var root = JsonNode.Parse(content);
            if (!(root is JsonObject))
                throw new InvalidOperationException("Root expected to be a JSON object.");
            return DeserializeResultSet(root, context, isHttp);
        }

        // ReSharper disable UnusedParameter.Local
        private IEnumerable<TResult> ParseInSingleColumnMode(DeserializationContext context, JsonNode root, string[] columnNames, TypeMapping[] jsonTypeMappings, bool isHttp)
        // ReSharper restore UnusedParameter.Local
        {
            if (columnNames.Count() != 1)
                throw new InvalidOperationException("The deserializer is running in single column mode, but the response included multiple columns which indicates a projection instead. If using the fluent Cypher interface, use the overload of Return that takes a lambda or object instead of single string. (The overload with a single string is for an identity, not raw query text: we can't map the columns back out if you just supply raw query text.)");

            var resultType = typeof(TResult);
            var isResultTypeANodeOrRelationshipInstance = resultType.GetTypeInfo().IsGenericType &&
                                       (resultType.GetGenericTypeDefinition() == typeof(Node<>) ||
                                        resultType.GetGenericTypeDefinition() == typeof(RelationshipInstance<>));
            var mapping = jsonTypeMappings.SingleOrDefault(m => m.ShouldTriggerForPropertyType(0, resultType));
            var newType = mapping == null ? resultType : mapping.DetermineTypeToParseJsonIntoBasedOnPropertyType(resultType);

            var dataArray = isHttp ? root["results"]?[0]?["data"] as JsonArray : root["data"] as JsonArray;
            if (dataArray == null)
                dataArray = !isHttp ? root["results"]?[0]?["data"] as JsonArray : root["data"] as JsonArray;
            var rows = dataArray.ToList();

            var dataPropertyNameInTransaction = "row";
            var results = rows.Select(row =>
                    {
                        if (inTransaction || isHttp)
                        {
                            var rowObject = row as JsonObject;
                            if (rowObject == null)
                                throw new InvalidOperationException("Expected the row to be a JSON object, but it wasn't.");

                            if (!rowObject.ContainsKey(dataPropertyNameInTransaction))
                                throw new InvalidOperationException("There is no row property in the JSON object.");

                            row = rowObject[dataPropertyNameInTransaction];
                        }

                        if (!(row is JsonArray))
                            throw new InvalidOperationException("Expected the row to be a JSON array of values, but it wasn't.");

                        var rowAsArray = (JsonArray) row;
                        if (rowAsArray.Count > 1)
                            throw new InvalidOperationException($"Expected the row to only have a single array value, but it had {rowAsArray.Count}.");

                        return rowAsArray;
                    }
                )
                .Where(row => row.Count != 0)
                .Select(row =>
                {
                    var elementToParse = row[0];
                    if (elementToParse is JsonObject elementObj)
                    {
                        var propertyNames = elementObj.Select(p => p.Key).ToArray();
                        var dataElementLooksLikeANodeOrRelationshipInstance =
                            new[] {"data", "self", "traverse", "properties"}.All(propertyNames.Contains);
                        if (!isResultTypeANodeOrRelationshipInstance &&
                            dataElementLooksLikeANodeOrRelationshipInstance)
                            elementToParse = elementToParse["data"];
                    }

                    var parsed = CommonDeserializerMethods.CreateAndMap(context, newType, elementToParse, jsonTypeMappings, 0);
                    return (TResult) (mapping == null ? parsed : mapping.MutationCallback(parsed));
                });

            return results;
        }

        IEnumerable<TResult> ParseInProjectionMode(DeserializationContext context, JsonNode root, string[] columnNames, TypeMapping[] jsonTypeMappings, bool isHttp)
        {
            var properties = typeof(TResult).GetProperties();
            var propertiesDictionary = properties.ToDictionary(p => p.Name);

            Func<JsonNode, TResult> getRow = null;

            var columnsWhichDontHaveSettableProperties = columnNames.Where(c => !propertiesDictionary.ContainsKey(c) || !propertiesDictionary[c].CanWrite).ToArray();
            if (columnsWhichDontHaveSettableProperties.Any())
            {
                var ctor = typeof(TResult).GetConstructors().FirstOrDefault(info =>
                {
                    var parameters = info.GetParameters();
                    if (parameters.Length != columnNames.Length) return false;
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var property = propertiesDictionary[columnNames[i]];
                        if (!parameters[i].ParameterType.IsAssignableFrom(property.PropertyType))
                            return false;
                    }
                    return true;
                });

                if (ctor != null)
                    getRow = token => ReadProjectionRowUsingCtor(context, token, propertiesDictionary, columnNames, jsonTypeMappings, ctor);

                if (getRow == null)
                {
                    var columnsCommaSeparated = string.Join(", ", columnsWhichDontHaveSettableProperties);
                    throw new ArgumentException($"The query response contains columns {columnsCommaSeparated} however {typeof(TResult).FullName} does not contain publicly settable properties to receive this data.", nameof(columnNames));
                }
            }
            else
            {
                getRow = token => ReadProjectionRowUsingProperties(context, token, propertiesDictionary, columnNames, jsonTypeMappings);
            }

            var dataArray = isHttp ? root["results"]?[0]?["data"] as JsonArray : root["data"] as JsonArray;
            if (dataArray == null)
                dataArray = !isHttp ? root["results"]?[0]?["data"] as JsonArray : root["data"] as JsonArray;

            var rows = dataArray.ToList();
            var dataPropertyNameInTransaction = "row";

            return inTransaction || isHttp
                ? rows.Select(row => row[dataPropertyNameInTransaction]).Select(getRow)
                : rows.Select(getRow);
        }

        TResult ReadProjectionRowUsingCtor(
            DeserializationContext context,
            JsonNode row,
            IDictionary<string, PropertyInfo> propertiesDictionary,
            IList<string> columnNames,
            IEnumerable<TypeMapping> jsonTypeMappings,
            ConstructorInfo ctor)
        {
            var rowArray = row as JsonArray;
            var coercedValues = rowArray
                .Select((cell, cellIndex) =>
                {
                    var columnName = columnNames[cellIndex];
                    var property = propertiesDictionary[columnName];
                    if (IsNullArray(property, cell)) return null;
                    return CommonDeserializerMethods.CoerceValue(context, property, cell, jsonTypeMappings, 0);
                })
                .ToArray();

            return (TResult)ctor.Invoke(coercedValues);
        }

        TResult ReadProjectionRowUsingProperties(
            DeserializationContext context,
            JsonNode row,
            IDictionary<string, PropertyInfo> propertiesDictionary,
            IList<string> columnNames,
            TypeMapping[] jsonTypeMappings)
        {
            var result = Activator.CreateInstance<TResult>();
            var rowArray = row as JsonArray;
            var cellIndex = 0;
            foreach (var cell in rowArray)
            {
                var columnName = columnNames[cellIndex++];
                var property = propertiesDictionary[columnName];
                if (IsNullArray(property, cell)) continue;
                CommonDeserializerMethods.SetPropertyValue(context, result, property, cell, jsonTypeMappings, 0);
            }
            return result;
        }

        static bool IsNullArray(PropertyInfo property, JsonNode cell)
        {
            var propertyType = property.PropertyType;
            var isEnumerable = propertyType.GetTypeInfo().IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            var isArrayOrEnumerable = isEnumerable || propertyType.IsArray;
            if (!isArrayOrEnumerable) return false;
            if (!(cell is JsonArray arr)) return false;
            var children = arr.ToList();
            return children.Any() && children.All(c => c == null || (c is JsonValue jv && jv.TryGetValue<object>(out var v) && v == null));
        }
    }
}
