using System;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using IBDTools.VMs;

namespace IBDTools.Workers {
    public class CardsClaimer : IWorker {
        public bool ClaimStandard, ClaimHeroic, ClaimEvent;
        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var tavern = new Tavern(context);
            //var rewards = new RewardsDialog(context);
            var claimed = false;
            do {
                claimed = false;
                cancellationToken.ThrowIfCancellationRequested();
                if (!tavern.IsScreenActive())
                    throw new InvalidOperationException("You're not at the tavern screen");
                statusUpdater($"{tavern.StandardCards} standard cards, {tavern.HeroicCards} heroic cards, {tavern.EventPoints} event points.");

                if (tavern.StandardCards > 0 && ClaimStandard) {
                    if (tavern.StandardCards >= 10)
                        await tavern.PressClaimStandard10Button(cancellationToken);
                    else
                        await tavern.PressClaimStandard1Button(cancellationToken);
                    while (!tavern.IsScreenActive())
                        await tavern.PressCloseButton(cancellationToken);
                    claimed = true;
                }

                if (tavern.HeroicCards > 0 && ClaimHeroic) {
                    if (tavern.HeroicCards >= 10)
                        await tavern.PressClaimHeroic10Button(cancellationToken);
                    else
                        await tavern.PressClaimHeroic1Button(cancellationToken);
                    while (!tavern.IsScreenActive())
                        await tavern.PressCloseButton(cancellationToken);
                    claimed = true;
                }

                if (tavern.EventPoints > 10 && ClaimEvent) {
                    if (tavern.EventPoints >= 100)
                        await tavern.PressClaimEvent10Button(cancellationToken);
                    else
                        await tavern.PressClaimEvent1Button(cancellationToken);
                    while (!tavern.IsScreenActive())
                        await tavern.PressCloseButton(cancellationToken);
                    claimed = true;
                }
            } while (claimed);
        }
    }
}
