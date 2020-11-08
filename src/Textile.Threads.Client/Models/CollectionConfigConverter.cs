using System;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using Google.Protobuf;

namespace Textile.Threads.Client.Models
{ 
    public class CollectionConfigConverter : ITypeConverter<CollectionConfig, Grpc.CollectionConfig>
    {
        public Grpc.CollectionConfig Convert(CollectionConfig source, Grpc.CollectionConfig destination, ResolutionContext context)
        {
            var config = new Grpc.CollectionConfig()
            {
                Name = source.Name,
                Schema = ByteString.CopyFromUtf8(JsonSerializer.Serialize(source.Schema))
            };

            if (source.Indexes != null)
            {
                var indexes = source.Indexes.Select(i => new Grpc.Index()
                {
                    Path = i.Path,
                    Unique = i.Unique
                }).ToArray();
                config.Indexes.AddRange(indexes);
            }

            return config;
        }
    }
}
