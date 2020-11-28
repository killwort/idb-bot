using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ShopEvent : Event {
        public static readonly uint[][] ColorPoints = new uint[][] {
            new[] {
                0x20555A5Du,
                0x20CEA27Bu,
                0x2056777Du,
                0x208F5C44u,
                0x20372114u,
                0x20915E45u,
                0x2032180Fu,
                0x20392018u,
                0x20381D17u,
            },
            new[] {
                0x102A507Du,
                0x1048668Bu,
                0x101F212Cu,
                0x1069675Eu,
                0x10B28644u,
                0x1063563Au,
                0x10594D56u,
                0x1067594Du,
                0x10584E4Cu,
            },
            new[] {
                0x20502535u,
                0x2084B0DCu,
                0x20492133u,
                0x2052242Du,
                0x2095422Du,
                0x20682C37u,
                0x201B1510u,
                0x20917555u,
                0x201B1510u,
            },
            new[] {
                0x202a507du,
                0x2048668bu,
                0x201f212cu,
                0x2069675eu,
                0x20b28644u,
                0x2063563au,
                0x20594d56u,
                0x2067594du,
                0x20584e4cu
            },
            new[] {
                0x204b2133u,
                0x20847e72u,
                0x20502533u,
                0x20402320u,
                0x20bb874du,
                0x20642c3au,
                0x20181410u,
                0x209c7d5au,
                0x20181410u
            }
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
