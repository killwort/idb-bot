using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class BossEvent : Event {
        public static readonly uint[][] ColorPoints = new uint[][] {

            new[] {
                0x20283A56u,
                0x2023395Bu,
                0x2024395Au,
                0x202A5273u,
                0x20896860u,
                0x202A5172u,
                0x20326C93u,
                0x20858B87u,
                0x20316C93u,
            },
            new[] {
                0x20225847u,
                0x201E5445u,
                0x20474647u,
                0x202C6647u,
                0x20583D31u,
                0x202A6548u,
                0x203B8352u,
                0x20A2B8B5u,
                0x203B8653u,
            },
            /*new[] {
                0x202E1E26u,
                0x204D2232u,
                0x20522634u,
                0x20692B37u,
                0x2057402Eu,
                0x20692B37u,
                0x208F373Fu,
                0x2080837Eu,
                0x20953B41u,
            },*/
            new[] {
                0x201D4E3Du,
                0x201F5340u,
                0x201F5441u,
                0x202F6648u,
                0x208D6A63u,
                0x202E6745u,
                0x203C8252u,
                0x20858B87u,
                0x203C8351u,
            },
            new[] {
                0x203A2E52u,
                0x203E3059u,
                0x203D3057u,
                0x20573C6Fu,
                0x2086645Cu,
                0x2051396Eu,
                0x20774B94u,
                0x20868D87u,
                0x20774A93u,
            }
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
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try {
                await battle.Activation(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token);
            } catch (TaskCanceledException) {
                await TryClose(context, cancellationToken);
                return;
            }

            await battle.Engage(cancellationToken);
            await Task.Delay(3000, cancellationToken);
        }
    }
}
