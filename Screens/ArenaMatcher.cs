using System;
using System.Drawing;
using System.Linq;

namespace IBDTools.Screens {
    public class ArenaMatcher : VerifyByTextScreen
//        SnapshotOnActivationScreen
    {
        public ArenaMatcher(GameContext context) : base(context) { }


        protected override string RequiredText => "Score";

        protected override Rectangle RequiredTextBox => new Rectangle(500, 469, 42, 19);

        //500,465
        private static readonly Rectangle MyPowerBox = new Rectangle(250, 175, 460, 19);
        private static readonly Rectangle TicketsLeftBox = new Rectangle(495, 68, 100, 19);
        private static readonly Point RefreshButton = new Point(723, 181);
        private static readonly Point CloseButton = new Point(788, 121);


        private static readonly Tuple<Rectangle, Rectangle, Rectangle, Point>[] OpponentBoxes = new[] {
            Tuple.Create(new Rectangle(337, 295, 158, 18), new Rectangle(300, 256, 200, 19), new Rectangle(500, 295, 120, 19), new Point(702, 285)),
            Tuple.Create(new Rectangle(337, 396, 158, 18), new Rectangle(300, 355, 200, 19), new Rectangle(500, 396, 120, 19), new Point(702, 385)),
            Tuple.Create(new Rectangle(337, 499, 158, 18), new Rectangle(300, 460, 200, 19), new Rectangle(500, 499, 120, 19), new Point(702, 485)),
        };

        public class Opponent {
            internal int Index;
            public string Name;
            public long Power;
            public long Score;
        }

        public long MyPower => Context.NumberFromBitmap(CurrentScreen, MyPowerBox);
        public long TicketsLeft => Context.NumberFromBitmap(CurrentScreen, TicketsLeftBox);
        public bool IsFastBattleEnabled {
            get => CurrentScreen.GetPixel(679, 216).G > 200;
            set {
                if (IsFastBattleEnabled != value)
                    Context.ClickAt(679, 216);
                CurrentScreen = Context.FullScreenshot();
            }
        }

        public Opponent[] Opponents =>
            OpponentBoxes.Select(
                (boxDef, i) => new Opponent {
                    Index = i,
                    Power = Context.NumberFromBitmap(CurrentScreen, boxDef.Item1),
                    Name = Context.TextFromBitmap(CurrentScreen, boxDef.Item2),
                    Score = Context.NumberFromBitmap(CurrentScreen, boxDef.Item3),
                }
            ).ToArray();

        public void GetNewOpponents() => Context.ClickAt(RefreshButton);

        public void EngageOpponent(Opponent opponent) => Context.ClickAt(OpponentBoxes[opponent.Index].Item4);
        public void Close() => Context.ClickAt(CloseButton);
    }
}
