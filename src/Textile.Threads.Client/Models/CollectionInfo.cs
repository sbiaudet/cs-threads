using System;
using System.Collections.Generic;
using System.Text.Json;
using Json.Schema;

namespace Textile.Threads.Client.Models
{

    public class CollectionInfo
    {

        private static readonly JsonSchema DefaultShema = JsonSchema.FromText("{\"properties\": { \"_id\": { \"type\": \"string\" } } }");

        public string Name { get; set; }
        public JsonSchema Schema { get; set; } = DefaultShema;
        public List<Grpc.Index> Indexes { get; set; } = new List<Grpc.Index>();
        public string WriteValidator { get; set; }
        public string ReadFilter { get; set; }
    }

}
