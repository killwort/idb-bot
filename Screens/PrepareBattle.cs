using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class PrepareBattle : VerifyByPointsScreen {
        private static readonly Point BattleButton = new Point(708, 427);

        public PrepareBattle(GameContext context) : base(context) { }

        //protected override Rectangle RequiredTextBox => new Rectangle(BattleButton, new Size(90, 33));
        //protected override string RequiredText => "Battle";
        protected override Tuple<Point, Color>[] RequiredPoints =>
            new[] {Tuple.Create(new Point(803, 429), Color.FromArgb(0xb0, 0x65, 0x4a)), Tuple.Create(new Point(258, 528), Color.FromArgb(0x35, 0x2d, 0x3c))};

        public Task Engage(CancellationToken cancellationToken) => Context.ClickAt(BattleButton, cancellationToken);
    }
}