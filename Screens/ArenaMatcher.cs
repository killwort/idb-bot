using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ArenaMatcher : VerifyByTextScreen
//        SnapshotOnActivationScreen
    {
        private static readonly Rectangle MyPowerBox = new Rectangle(250, 175, 460, 19);
        private static readonly Rectangle TicketsLeftBox = new Rectangle(493, 68, 100, 19);
        private static readonly Point RefreshButton = new Point(723, 181);
        private static readonly Point CloseButton = new Point(788, 121);

        private static readonly Tuple<Rectangle, Rectangle, Rectangle, Point>[] OpponentBoxes = {
            Tuple.Create(new Rectangle(337, 295, 158, 18), new Rectangle(300, 256, 200, 19), new Rectangle(500, 295, 120, 19), new Point(702, 285)),
            Tuple.Create(new Rectangle(337, 396, 158, 18), new Rectangle(300, 355, 200, 19), new Rectangle(500, 396, 120, 19), new Point(702, 385)),
            Tuple.Create(new Rectangle(337, 499, 158, 18), new Rectangle(300, 460, 200, 19), new Rectangle(500, 499, 120, 19), new Point(702, 485))
        };

        public ArenaMatcher(GameContext context) : base(context) { }

        protected override string RequiredText => "Score";

        protected override Rectangle RequiredTextBox => new Rectangle(500, 469, 42, 19);

        public long MyPower => Context.NumberFromBitmap(CurrentScreen, MyPowerBox);
        public long TicketsLeft => Context.NumberFromBitmap(CurrentScreen, TicketsLeftBox);
        public bool IsFastBattleEnabled {
            get {
                var px=CurrentScreen.GetPixel(679, 216);
                return px.G > 200 && px.B < 150 && px.R < 150;
            }
        }

        public Opponent[] Opponents =>
            OpponentBoxes.Select(
                (boxDef, i) => new Opponent {
                    Index = i,
                    Power = Context.NumberFromBitmap(CurrentScreen, boxDef.Item1),
                    Name = Context.TextFromBitmap(CurrentScreen, boxDef.Item2),
                    Score = Context.NumberFromBitmap(CurrentScreen, boxDef.Item3)
                }
            ).ToArray();

        public async Task ToggleFastBattle(bool value, CancellationToken cancellationToken) {
            if (IsFastBattleEnabled != value) {
                await Context.ClickAt(679, 216, cancellationToken, 0);
                CurrentScreen = Context.FullScreenshot();
            }
        }

        public Task GetNewOpponents(CancellationToken cancellationToken) => Context.ClickAt(RefreshButton, cancellationToken);

        public Task EngageOpponent(Opponent opponent, CancellationToken cancellationToken) => Context.ClickAt(OpponentBoxes[opponent.Index].Item4, cancellationToken);
        public Task Close(CancellationToken cancellationToken) => Context.ClickAt(CloseButton, cancellationToken);
        public void Refresh() => CurrentScreen = Context.FullScreenshot();
    }
}
