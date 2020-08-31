using System;
using System.Drawing;
using System.Linq;

namespace IBDTools.Screens {
    public abstract class VerifyByPointsScreen : SnapshotOnActivationScreen {
        protected VerifyByPointsScreen(GameContext context) : base(context) { }

        protected abstract Tuple<Point, Color>[] RequiredPoints { get; }
        protected virtual int MaxColorDivergence { get; } = 30;

        public override bool IsScreenActive(Bitmap screen) => RequiredPoints.All(ptc => screen.VerifyPixelColor(ptc.Item1, ptc.Item2, MaxColorDivergence));
    }
}
