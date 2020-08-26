using System;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Workers {
    public interface IWorker {
        Task Run(GameContext context, Action<string> statusUpdater, CancellationToken cancellationToken);
    }
}
