using System;
using System.Threading.Tasks;

namespace Textile.Threads.Client
{
    public interface IThreadClientFactory
    {
        Task<IThreadClient> CreateClientAsync();
    }
}
