using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Startup
{
    public interface IInstrumentStartup
    {
        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
