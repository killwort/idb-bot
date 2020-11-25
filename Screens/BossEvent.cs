using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class BossEvent : Event {
        public static readonly uint[] ColorPoints = new[] {
            0xFF1E5344u,
            0xFF1A5043u,
            0xFF55514Du,
            0xFF30684Bu,
            0x0a4A2818u,
            0xFF4A3026u,
            0xFF3C8253u,
            0x0a899A97u,
            0xFF3B8656u,
        };

        public BossEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogButton1, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(600, 430, cancellationToken);
            await Task.Delay(200, cancellationToken);
            var battle = new PrepareBattle(context);
            await battle.Activation(cancellationToken);
            await battle.Engage(cancellationToken);
            await Task.Delay(3000, cancellationToken);
        }
    }
}