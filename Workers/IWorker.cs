using System;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public interface IWorker {
        Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken);
    }
}