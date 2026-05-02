using System;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    class RootApiResponse
    {
        private string _transactionFormat;
        private string _transaction;

        [JsonPropertyName("transaction")]
        public string Transaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _transactionFormat = null;
            }
        }

        internal string TransactionFormat
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_transactionFormat))
                    _transactionFormat = Transaction?.Replace("databaseName", "0");

                return _transactionFormat;
            } 
        }

        // [JsonPropertyName("cypher")]
        // public string Cypher { get; set; }

        [JsonPropertyName("batch")]
        public string Batch { get; set; }

        [JsonPropertyName("node")]
        public string Node { get; set; }

        [JsonPropertyName("relationship")]
        public string Relationship { get; set; }
        
        [JsonPropertyName("node_index")]
        public string NodeIndex { get; set; }

        [JsonPropertyName("relationship_index")]
        public string RelationshipIndex { get; set; }

        [JsonPropertyName("reference_node")]
        public string ReferenceNode { get; set; }

        [JsonPropertyName("extensions_info")]
        public string ExtensionsInfo { get; set; }

        [JsonPropertyName("neo4j_version")]
        public string Neo4jVersion { get; set; }

        [JsonPropertyName("neo4j_edition")]
        public string Neo4jEdition { get; set; }

        [JsonPropertyName("bolt_direct")]
        public string BoltDirect { get; set; }

        [JsonPropertyName("bolt_routing")]
        public string BoltRouting { get; set; }

        [JsonPropertyName("cluster")]
        public string Cluster { get; set; }

        /// <summary>
        /// Returns a structured representation of the Neo4j server version, but only with partial data.
        /// The version type (milestone, preview, release candidate, stable) is not taken in to account,
        /// so both 1.9.M01, 1.9.RC1 and 1.9.1 will all return 1.9.0.1.
        /// </summary>
        [JsonIgnore]
        public Version Version => GetVersion(Neo4jVersion);

        internal static Version GetVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return new Version(0, 0);

            var numericalVersionString = Regex.Replace(
                version,
                @"(?<major>\d*)[.](?<minor>\d*)[.]?(M(?<build>\d*)|RC(?<build>\d*)?).*",
                "${major}.${minor}.0.${build}");

            numericalVersionString = Regex.Replace(
                numericalVersionString,
                @"(?<major>\d*)[.](?<minor>\d*)-.*",
                "${major}.${minor}");

            Version result;
            var parsed = Version.TryParse(numericalVersionString, out result);

            return parsed ? result : new Version(0, 0);
        }

        internal void TrimUriFromProperties(string absoluteUri)
        {
            var baseUriLengthToTrim = absoluteUri.Length - 1;

            Batch = (string.IsNullOrWhiteSpace(Batch)) ? null : Batch.Substring(baseUriLengthToTrim);
            Node = (string.IsNullOrWhiteSpace(Node)) ? null : Node.Substring(baseUriLengthToTrim);
            NodeIndex = (string.IsNullOrWhiteSpace(NodeIndex)) ? null : NodeIndex.Substring(baseUriLengthToTrim);
            Relationship = "/relationship"; //Doesn't come in on the Service Root
            RelationshipIndex = (string.IsNullOrWhiteSpace(RelationshipIndex)) ? null : RelationshipIndex.Substring(baseUriLengthToTrim);
            ExtensionsInfo = (string.IsNullOrWhiteSpace(ExtensionsInfo)) ? null : ExtensionsInfo.Substring(baseUriLengthToTrim);

            Transaction = (string.IsNullOrWhiteSpace(Transaction)) ? null : Transaction.Substring(baseUriLengthToTrim);
            //Cypher = (string.IsNullOrWhiteSpace(Cypher)) ? string.Empty : Cypher.Substring(baseUriLengthToTrim);
            Cluster = (string.IsNullOrWhiteSpace(Cluster)) ? null : Cluster.Substring(baseUriLengthToTrim);
        }
    }
}
