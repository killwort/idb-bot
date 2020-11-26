using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ChestEvent : Event {
        public static uint[][] ColorPoints = new uint[][] {
            new[] {
                0x20184F42u,
                0x20D1A636u,
                0x201D5243u,
                0x2020282Au,
                0x207C797Bu,
                0x20231F1Du,
                0x20542E18u,
                0x205B95B2u,
                0x202478B3u,
            },
            new[] {
                0x204C4132u,
                0x20335466u,
                0x204D4332u,
                0x205F5237u,
                0x20392E27u,
                0x2068593Bu,
                0x20907C44u,
                0x203E3134u,
                0x208F7B42u,
            },
            new[] {
                0x201C4F43u,
                0x20362A24u,
                0x201A4D42u,
                0x2025503Au,
                0x2056555Eu,
                0x202A6147u,
                0x203A8052u,
                0x203B393Bu,
                0x203A7F52u,
            },
            new[] {
                0x20293D57u,
                0x204A331Eu,
                0x20253A58u,
                0x202D5571u,
                0x20464446u,
                0x202B5370u,
                0x20346E94u,
                0x20111820u,
                0x20336F97u,
            },
            new[] {
                0x20253A59u,
                0x20E9C835u,
                0x20223759u,
                0x20212830u,
                0x206A6668u,
                0x20161C23u,
                0x20673A16u,
                0x2075C3E5u,
                0x201A3E58u,
            },
            new[] {
                0x201F5443u,
                0x20D4AA32u,
                0x201F5449u,
                0x2027343Bu,
                0x20737573u,
                0x202C2626u,
                0x2036754Au,
                0x2009194Cu,
                0x202C82D8u,
            },
            new[] {
                0x20283C5Bu,
                0x20E6C845u,
                0x20273B59u,
                0x201B1415u,
                0x207E7C7Eu,
                0x20192834u,
                0x207C451Bu,
                0x209AD2E0u,
                0x202A597Au,
            },
            new[] {
                0x20235445u,
                0x20E8C735u,
                0x20215243u,
                0x20222A2Cu,
                0x20726F71u,
                0x20252324u,
                0x202C2819u,
                0x20143F60u,
                0x203DBADDu,
            },
            new[] {
                0x20273E5Du,
                0x20403026u,
                0x20223A5Au,
                0x202E5877u,
                0x20565356u,
                0x201F1F1Fu,
                0x20337096u,
                0x2028526Du,
                0x2030709Cu,
            },
            new[] {
                0x20273E5Du,
                0x20403026u,
                0x20223A5Au,
                0x202E5877u,
                0x20565356u,
                0x201F1F1Fu,
                0x20337096u,
                0x2028526Du,
                0x2030709Cu,
            },
            new[] {
                0x201B4F43u,
                0x20ECCE37u,
                0x201A544Du,
                0x20212830u,
                0x20706C6Fu,
                0x2016201Au,
                0x206B3B17u,
                0x207AC3E0u,
                0x201A414Cu,
            },
            new[] {
                0x20293D5Du,
                0x20D8AE36u,
                0x20263B5Bu,
                0x20232E32u,
                0x207B797Bu,
                0x20252425u,
                0x201B394Du,
                0x20034475u,
                0x2038BFEFu,
            },
            new[] {
                0x203D2E58u,
                0x20433224u,
                0x203B2C56u,
                0x20573A6Fu,
                0x204C4B4Fu,
                0x2056386Fu,
                0x20764995u,
                0x20261F2Au,
                0x20774892u,
            }
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
