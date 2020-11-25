using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class EnergyBuster : IWorker {
        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var hall = new EventHall(context);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!hall.IsScreenActive())
                    throw new InvalidOperationException("You're not at the event hall screen");
                statusUpdater("Looking for active events...");
                await hall.ToggleFastBattle(true, cancellationToken);
                var events = hall.Events;
                var ev = events.FirstOrDefault(x => !x.Ignore);
                if (ev != null) {
                    statusUpdater($"Resolving {ev.GetType().Name}...");
                    await hall.ResolveEvent(ev, cancellationToken);
                } else {
                    statusUpdater("No events found, searching for one...");
                    await hall.FindMoreEvents(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
