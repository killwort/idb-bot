using System;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class Mapper : IWorker {
        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) { }
    }
}