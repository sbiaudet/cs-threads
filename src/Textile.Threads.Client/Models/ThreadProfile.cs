using System;
using AutoMapper;

namespace Textile.Threads.Client.Models
{
    public class ThreadProfile : Profile
    {
        public ThreadProfile()
        {
            CreateMap<CollectionInfo, Grpc.CollectionConfig>().ConvertUsing<CollectionInfoConverter>();
            CreateMap<Grpc.GetCollectionInfoReply, CollectionInfo>().ConvertUsing<CollectionInfoConverter>();
        }
    }
}
