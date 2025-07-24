using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IPersistenceManager : IHostedService
    {
        void Register(object persistence);
        Task RegisterAsync(object persistence);
    }
}
