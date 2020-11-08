using System;
using System.Collections.Generic;
using System.Text.Json;
using Json.Schema;

namespace Textile.Threads.Client.Models
{

    public class CollectionConfig
    {

        public static JsonSchema DefaultShema = JsonSchema.FromText("{\"properties\": { \"_id\": { \"type\": \"string\" } } }");

        public delegate bool WriteValidatorFunc(string writer, Event @event, object instance);
        public delegate object ReadFilterFunc(string reader, object instance);

        public string Name { get; set; }
        public JsonSchema Schema { get; set; } = DefaultShema;
        public List<Grpc.Index> Indexes { get; set; } = new List<Grpc.Index>();
        public WriteValidatorFunc WriteValidator { get; set; }
        public ReadFilterFunc ReadFilter { get; set; }
    }

}
