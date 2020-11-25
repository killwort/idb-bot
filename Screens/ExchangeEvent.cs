using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ExchangeEvent : Event {
        public static readonly uint[] ColorPoints = new[] {
            0xFF4C2032u,
            0x301B3364u,
            0xFF4E263Au,
            0x30101848u,
            0x307CDDF0u,
            0x3026488Fu,
            0xFF8F3640u,
            0x30213F9Au,
            0xFF923A42u,
        };
        public ExchangeEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogDeleteButton, cancellationToken);
            await Task.Delay(200, cancellationToken);
        }

        public override bool Ignore => true;
    }
}