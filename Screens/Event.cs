using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public abstract class Event {
        protected static readonly Point DialogButton1 = new Point(505, 308);
        protected static readonly Point DialogButton2 = new Point(505, 400);
        protected static readonly Point DialogDeleteButton = new Point(521, 216);
        protected static readonly Point DialogDeleteOkButton = new Point(368, 440);
        public Rectangle ClickBox { get; }

        public Event(Rectangle clickBox) { ClickBox = clickBox; }

        public abstract Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken);

        protected async Task Activate(GameContext context, CancellationToken cancellationToken) {
            await context.ClickAt(ClickBox.Left + ClickBox.Width / 2, ClickBox.Top + ClickBox.Height / 2, cancellationToken);
            await Task.Delay(200, cancellationToken);
        }

        protected async Task Dismiss(GameContext context, CancellationToken cancellationToken) {
            await context.ClickAt(DialogDeleteButton, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogDeleteOkButton, cancellationToken);
            await Task.Delay(200, cancellationToken);
        }

        public virtual bool Ignore => false;
    }
}