using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface IPersistenceManager : IHostedService
    {
        Task RegisterAsync(object persistence);
    }
}
