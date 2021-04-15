using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class WallBattle : VerifyByPointsScreen {

        private static readonly Point TwoMirrorsButton = new Point(308, 429);
        private static readonly Point OneMirroButton = new Point(523,429);
        public WallBattle(GameContext context) : base(context) { }

        protected override Tuple<Point, Color>[] RequiredPoints =>
            new[] {
                Tuple.Create(new Point(3, 5), Color.FromArgb(102, 86, 110)),
                Tuple.Create(new Point(118, 638), Color.FromArgb(86, 64, 95)),
                Tuple.Create(new Point(448, 616), Color.FromArgb(255, 255, 198))
            };
        protected Tuple<Point, Color>[] RequiredPointsFailure =>
            new[] {
                Tuple.Create(new Point(3, 5), Color.FromArgb(32, 27, 34)),
                Tuple.Create(new Point(118, 638), Color.FromArgb(28, 20, 30)),
                Tuple.Create(new Point(448, 616), Color.FromArgb(79, 79, 58)),
                Tuple.Create(TwoMirrorsButton, Color.FromArgb(146,91,72)),
                Tuple.Create(OneMirroButton, Color.FromArgb(38,85,118)),
            };
        public bool IsFailureDialogActive() => RequiredPointsFailure.All(ptc => Context.FullScreenshot().VerifyPixelColor(ptc.Item1, ptc.Item2, MaxColorDivergence));

        public Task PressUse1Mirror(CancellationToken cancellationToken) => Context.ClickAt(OneMirroButton, cancellationToken);
        public Task PressUse2Mirrors(CancellationToken cancellationToken) => Context.ClickAt(TwoMirrorsButton, cancellationToken);
    }
}
