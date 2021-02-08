using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ShopEvent : Event {
        public ShopEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task<bool> ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await context.ClickAt(DialogButton2, cancellationToken);
            var battle = new PrepareBattle(context);
            await context.ClickAt(400, 430, cancellationToken);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try {
                await battle.Activation(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token);
            } catch (TaskCanceledException) {
                await TryClose(context, cancellationToken);
                return false;
            }
            await battle.Engage(cancellationToken);
            await WaitCombatEnd(context, cancellationToken);
            return true;
        }
    }
}
