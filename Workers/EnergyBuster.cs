using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class EnergyBuster : IWorker {
        public static int n = 0;
        public bool DismissExchanges = false;
        public bool DismissBarters = false;

        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var hall = new EventHall(context);
            var ignoredTypes = new List<Type>();
            if (!DismissBarters) ignoredTypes.Add(typeof(BarterEvent));
            if (!DismissExchanges) ignoredTypes.Add(typeof(ExchangeEvent));

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!hall.IsScreenActive())
                    throw new InvalidOperationException("You're not at the event hall screen");
                statusUpdater("Looking for active events...");
                await hall.ToggleFastBattle(true, cancellationToken);
                var events = hall.Events;
                var ev = events.FirstOrDefault(x => !ignoredTypes.Contains(x.GetType()));

                try {
                    if (ev != null) {
                        statusUpdater($"Resolving {ev.GetType().Name}...");
                        if (!await hall.ResolveEvent(ev, cancellationToken)) {
                            cancellationToken.ThrowIfCancellationRequested();
                            await hall.FindMoreEvents(cancellationToken);
                        }
                    } else {
                        statusUpdater("No events found, searching for one...");
                        await hall.FindMoreEvents(cancellationToken);
                    }
                } catch (OperationCanceledException) {
                    // prevent unintended cancellation
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
