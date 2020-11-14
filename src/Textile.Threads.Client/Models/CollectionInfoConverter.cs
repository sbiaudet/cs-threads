using System;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using Google.Protobuf;
using Json.Schema;

namespace Textile.Threads.Client.Models
{ 
    public class CollectionInfoConverter :
        ITypeConverter<CollectionInfo, Grpc.CollectionConfig>,
        ITypeConverter<Grpc.GetCollectionInfoReply, CollectionInfo>
    {
        public Grpc.CollectionConfig Convert(CollectionInfo source, Grpc.CollectionConfig destination, ResolutionContext context)
        {
            var config = new Grpc.CollectionConfig()
            {
                Name = source.Name,
                Schema = ByteString.CopyFromUtf8(JsonSerializer.Serialize(source.Schema)),
                WriteValidator = source.WriteValidator ?? string.Empty,
                ReadFilter = source.ReadFilter ?? string.Empty
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

        public CollectionInfo Convert(Grpc.GetCollectionInfoReply source, CollectionInfo destination, ResolutionContext context)
        {
            var config = new CollectionInfo()
            {
                Name = source.Name,
                Schema = JsonSerializer.Deserialize<JsonSchema>(source.Schema.ToByteArray()),
                WriteValidator = source.WriteValidator,
                ReadFilter = source.ReadFilter,
                Indexes = source.Indexes.ToList()
            };

            return config;
        }
    }
}
