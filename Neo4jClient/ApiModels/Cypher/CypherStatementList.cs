using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neo4jClient.Cypher;

namespace Neo4jClient.ApiModels.Cypher
{
    /// <summary>
    /// Serializes CypherStatementList as {"statements": [...]} regardless of IList implementation.
    /// </summary>
    class CypherStatementListConverter : JsonConverter<CypherStatementList>
    {
        public override CypherStatementList Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
        {
            throw new System.NotImplementedException("Deserialization of CypherStatementList is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, CypherStatementList value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("statements");
            JsonSerializer.Serialize(writer, value.Statements, options);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Represents the collection of Cypher statements that are going to be sent through a transaction.
    /// </summary>
    [JsonConverter(typeof(CypherStatementListConverter))]
    class CypherStatementList : IList<CypherTransactionStatement>
    {
        private readonly IList<CypherTransactionStatement> _statements;

        public CypherStatementList()
        {
            _statements = new List<CypherTransactionStatement>();
        }

        public CypherStatementList(IEnumerable<CypherQuery> queries)
        {
            _statements = queries
                .Select(query => new CypherTransactionStatement(query))
                .ToList();
        }

        [JsonPropertyName("statements")]
        public IList<CypherTransactionStatement> Statements => _statements;

        public IEnumerator<CypherTransactionStatement> GetEnumerator()
        {
            return _statements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _statements).GetEnumerator();
        }

        public void Add(CypherTransactionStatement item)
        {
            _statements.Add(item);
        }

        public void Clear()
        {
            _statements.Clear();
        }

        public bool Contains(CypherTransactionStatement item)
        {
            return _statements.Contains(item);
        }

        public void CopyTo(CypherTransactionStatement[] array, int arrayIndex)
        {
            _statements.CopyTo(array, arrayIndex);
        }

        public bool Remove(CypherTransactionStatement item)
        {
            return _statements.Remove(item);
        }

        [JsonIgnore]
        public int Count
        {
            get { return _statements.Count; }
        }

        [JsonIgnore]
        public bool IsReadOnly
        {
            get { return _statements.IsReadOnly; }
        }

        public int IndexOf(CypherTransactionStatement item)
        {
            return _statements.IndexOf(item);
        }

        public void Insert(int index, CypherTransactionStatement item)
        {
            _statements.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _statements.RemoveAt(index);
        }

        [JsonIgnore]
        public CypherTransactionStatement this[int index]
        {
            get { return _statements[index]; }
            set { _statements[index] = value; }
        }
    }
}
