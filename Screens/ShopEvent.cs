using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ShopEvent : Event {
        public static readonly uint[] ColorPoints = new[] {
            0x20555A5Du,
            0x20CEA27Bu,
            0x2056777Du,
            0x208F5C44u,
            0x20372114u,
            0x20915E45u,
            0x2032180Fu,
            0x20392018u,
            0x20381D17u,
        };

        public ShopEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogButton2, cancellationToken);
            await Task.Delay(200, cancellationToken);
            var battle = new PrepareBattle(context);
            await context.ClickAt(400, 430, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await battle.Activation(cancellationToken);
            await battle.Engage(cancellationToken);
            await Task.Delay(3000, cancellationToken);
        }
    }
}