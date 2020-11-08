using System;
using AutoMapper;

namespace Textile.Threads.Client.Models
{
    public class ThreadProfile : Profile
    {
        public ThreadProfile()
        {
            CreateMap<Models.CollectionConfig, Grpc.CollectionConfig>().ConvertUsing<CollectionConfigConverter>();
        }
    }
}
