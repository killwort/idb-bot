using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ExchangeEvent : Event {
        public ExchangeEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await context.ClickAt(DialogDeleteButton, cancellationToken);
        }

        public override bool Ignore => true;
    }
}
