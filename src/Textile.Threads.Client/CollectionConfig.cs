using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Textile.Threads.Client
{

    public class CollectionConfig
    {
        public delegate bool WriteValidatorFunc(string writer, Event @event, object instance);
        public delegate object ReadFilterFunc(string reader, object instance);

        public string Name { get; set; }
        public JsonDocument Schema { get; set; }
        public List<Grpc.Index> Indexes { get; set; } = new List<Grpc.Index>();
        public WriteValidatorFunc WriteValidator { get; set; }
        public ReadFilterFunc ReadFilter { get; set; }
    }

}
