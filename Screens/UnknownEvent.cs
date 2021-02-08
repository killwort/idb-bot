using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class UnknownEvent : Event {
        public UnknownEvent(Rectangle clickBox) : base(clickBox) { }

        public override bool Ignore => true;
        public override Task<bool> ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
