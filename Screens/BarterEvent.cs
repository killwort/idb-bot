using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class BarterEvent : Event {
        public static uint[] ColorPoints = new[] {
            0x1a3F3059u,
            0x1a7D6349u,
            0x1a40315Au,
            0x1a5C3D74u,
            0x1a937249u,
            0x1a573A73u,
            0x1a9F604Du,
            0x1a9A6156u,
            0x1a794A92u
        };

        public BarterEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Dismiss(context, cancellationToken);
        }
    }
}