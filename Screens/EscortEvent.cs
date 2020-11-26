using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class EscortEvent : Event {
        public static readonly uint[][] ColorPoints = new uint[][] {
            new[] {
                0x20215444u,
                0x20937F3Eu,
                0x201E5345u,
                0x202B6547u,
                0x20413326u,
                0x202F674Au,
                0x203B8253u,
                0x2095804Bu,
                0x203A8755u,
            },
            new[] {
                0x103D2F59u,
                0x10EDDC99u,
                0x10403159u,
                0x1054386Du,
                0x10433A2Au,
                0x10573B71u,
                0x10754994u,
                0x10756238u,
                0x10794B92u,
            },
            new[] {
                0x201E5344u,
                0x20D1BC7Bu,
                0x201A4F43u,
                0x202D664Au,
                0x2042392Bu,
                0x202B6548u,
                0x203C8353u,
                0x206C5A32u,
                0x20398452u,
            },
            new [] {
               0x20243A59u,
               0x20E4D490u,
               0x20233959u,
               0x2027506Fu,
               0x20433A2Cu,
               0x20274F70u,
               0x20316C95u,
               0x20837041u,
               0x20326E97u,
            },
            new[] {
                0x20403258u,
                0x20CCC475u,
                0x20403157u,
                0x20553A6Fu,
                0x20453929u,
                0x20583B6Fu,
                0x20734892u,
                0x20A68F55u,
                0x20784A93u,
            }
        };

        public EscortEvent(Rectangle clickBox) : base(clickBox) { }

        public override async Task ResolveEvent(EventHall eventHall, GameContext context, CancellationToken cancellationToken) {
            await Activate(context, cancellationToken);
            await Task.Delay(200, cancellationToken);
            await context.ClickAt(DialogButton1, cancellationToken);
            await Task.Delay(200, cancellationToken);
            var battle = new PrepareBattle(context);
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try {
                await battle.Activation(CancellationTokenSource.CreateLinkedTokenSource(ct.Token, cancellationToken).Token);
            } catch (TaskCanceledException) {
                //Fuck, we've mistaken, it is a boss event!
                await context.ClickAt(600, 430, cancellationToken);
                await Task.Delay(200, cancellationToken);
                await battle.Activation(cancellationToken);
            }

            await battle.Engage(cancellationToken);
            await Task.Delay(3000, cancellationToken);
        }
    }
}
