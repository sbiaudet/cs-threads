using System;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using Google.Protobuf;
using Json.Schema;
using Textile.Threads.Client.Grpc;

namespace Textile.Threads.Client.Models
{
    public class CollectionInfoConverter :
        ITypeConverter<CollectionInfo, Grpc.CollectionConfig>,
        ITypeConverter<Grpc.GetCollectionInfoReply, CollectionInfo>
    {
        public CollectionConfig Convert(CollectionInfo source, CollectionConfig destination, ResolutionContext context)
        {
            CollectionConfig config = new()
            {
                Name = source.Name,
                Schema = ByteString.CopyFromUtf8(JsonSerializer.Serialize(source.Schema)),
                WriteValidator = source.WriteValidator ?? string.Empty,
                ReadFilter = source.ReadFilter ?? string.Empty
            };

            if (source.Indexes != null)
            {
                Grpc.Index[] indexes = source.Indexes.Select(i => new Grpc.Index()
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
            CollectionInfo config = new()
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
