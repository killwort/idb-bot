using System;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class TreasureMapLooter : IWorker {
        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var maps = new TreasureMaps(context);
            var rewards = new RewardsDialog(context);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!maps.IsScreenActive())
                    throw new InvalidOperationException("You're not at the treasure maps screen");
                statusUpdater("Looting...");
                await maps.PressLootButton(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                statusUpdater("Getting rewards...");
                await rewards.Activation(cancellationToken);
                await rewards.PressOkButton(cancellationToken);
            }
        }
    }
}