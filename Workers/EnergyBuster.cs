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

                /*foreach (var eev in events) {
                    using (var sbm = new Bitmap(eev.ClickBox.Width, eev.ClickBox.Height)) {
                        using (var dc = Graphics.FromImage(sbm)) {
                            dc.DrawImage(hall.Screen, 0, 0, eev.ClickBox, GraphicsUnit.Pixel);
                        }

                        var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", eev.GetType().Name);
                        if (!Directory.Exists(file))
                            Directory.CreateDirectory(file);

                        using (var ms = new MemoryStream()) {
                            sbm.Save(ms, ImageFormat.Png);
                            ms.Position = 0;
                            var hash = MD5.Create().ComputeHash(ms.ToArray()).Aggregate("", (s, b) => s + b.ToString("x2"));
                            ms.Position = 0;
                            using (var fs = File.Create(Path.Combine(file, $"event-{hash}.png"))) {
                                ms.CopyTo(fs);
                            }
                        }
                    }
                }*/

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

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
