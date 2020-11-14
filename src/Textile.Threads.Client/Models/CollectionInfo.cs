using System;
using System.Collections.Generic;
using System.Text.Json;
using Json.Schema;

namespace Textile.Threads.Client.Models
{

    public class CollectionInfo
    {

        public static JsonSchema DefaultShema = JsonSchema.FromText("{\"properties\": { \"_id\": { \"type\": \"string\" } } }");

        public string Name { get; set; }
        public JsonSchema Schema { get; set; } = DefaultShema;
        public List<Grpc.Index> Indexes { get; set; } = new List<Grpc.Index>();
        public String WriteValidator { get; set; }
        public String ReadFilter { get; set; }
    }

}
