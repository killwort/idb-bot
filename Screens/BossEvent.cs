using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class BossEvent : Event {
        public BossEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await context.ClickAt(DialogButton1, cancellationToken);
            await context.ClickAt(600, 430, cancellationToken);
            var battle = new PrepareBattle(context);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try {
                await battle.Activation(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token);
            } catch (TaskCanceledException) {
                await TryClose(context, cancellationToken);
                return;
            }

            await battle.Engage(cancellationToken);
            await WaitCombatEnd(context, cancellationToken);
        }
    }
}
