using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class EscortEvent : Event {
        public static readonly uint[] ColorPoints = new[] {
            0xFF215444u,
            0x3a937F3Eu,
            0xFF1E5345u,
            0xFF2B6547u,
            0x3a413326u,
            0xFF2F674Au,
            0xFF3B8253u,
            0x3a95804Bu,
            0xFF3A8755u,
        };

        public EscortEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogButton1, cancellationToken);
            await Task.Delay(200, cancellationToken);
            var battle = new PrepareBattle(context);
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try {
                await battle.Activation(CancellationTokenSource.CreateLinkedTokenSource(ct.Token, cancellationToken).Token);
            } catch (TaskCanceledException) {
                //Fuck, we've mistaken, it is a boss event!
                await context.ClickAt(600, 430, cancellationToken);
                await Task.Delay(200, cancellationToken);
                await battle.Activation(cancellationToken);
            }

            await battle.Engage(cancellationToken);
            await Task.Delay(3000, cancellationToken);
        }
    }
}