using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IBDTools.Screens;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class WallBattler : IWorker {
        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var wall = new WallBattle(context);
            if (!wall.IsScreenActive())
                throw new InvalidOperationException("You're not in the wall combat");
            var lastOneMirror = DateTime.UtcNow;
            var strange = 0;

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                statusUpdater("Waiting for heroes to die...");
                await wall.Deactivation(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!wall.IsFailureDialogActive()) {
                    if (strange++ > 50)
                        throw new Exception("Desynchronized from battle");
                    continue;
                }

                strange = 0;
                if ((DateTime.UtcNow - lastOneMirror).TotalMinutes > 15) {
                    statusUpdater("Respawning...");
                    lastOneMirror = DateTime.UtcNow;
                    await wall.PressUse1Mirror(cancellationToken);
                } else {
                    statusUpdater("Respawning...");
                    await wall.PressUse2Mirrors(cancellationToken);
                }

                context.MoveCursor(10, 10);
            }
        }
    }
}
