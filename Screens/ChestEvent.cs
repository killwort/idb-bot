using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ChestEvent : Event {
        public static uint[] ColorPoints = new[] {
            0xFF184F42u,
            0x20D1A636u,
            0xFF1D5243u,
            0x0a20282Au,
            0x0a7C797Bu,
            0x15231F1Du,
            0xFF542E18u,
            0x305B95B2u,
            0x302478B3u,
        };

        public static uint[] ColorPoints4 = new[] {
            0x104C4132u,
            0x10335466u,
            0x104D4332u,
            0x105F5237u,
            0x10392E27u,
            0x1068593Bu,
            0x10907C44u,
            0x103E3134u,
            0x108F7B42u,
        };

        public static uint[] ColorPoints1 = new[] {
            0x101C4F43u,
            0x10362A24u,
            0x101A4D42u,
            0x1025503Au,
            0x1056555Eu,
            0x102A6147u,
            0x103A8052u,
            0x103B393Bu,
            0x103A7F52u,
        };

        public ChestEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogButton1, cancellationToken);
            await Task.Delay(200, cancellationToken);
        }
    }
}