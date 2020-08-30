using System;
using System.Drawing;

namespace IBDTools.Screens {
    public abstract class VerifyByPointsScreen : SnapshotOnActivationScreen {
        protected VerifyByPointsScreen(GameContext context) : base(context) { }

        protected abstract Tuple<Point, Color>[] RequiredPoints { get; }
        protected virtual int MaxColorDivergence { get; } = 30;

        public override bool IsScreenActive(Bitmap screen) {
            foreach (var ptc in RequiredPoints) {
                var cl = screen.GetPixel(ptc.Item1.X, ptc.Item1.Y);
                if (Math.Abs(cl.B - ptc.Item2.B) + Math.Abs(cl.R - ptc.Item2.R) + Math.Abs(cl.G - ptc.Item2.G) > MaxColorDivergence) return false;
            }

            return true;
        }
    }
}