using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class Tavern : VerifyByPointsScreen {
        private static readonly Rectangle StandardCardsBox = new Rectangle(143, 280, 88, 24);
        private static readonly Rectangle HeroicCardsBox = new Rectangle(460,   280, 88, 24);
        private static readonly Rectangle EventPointsBox = new Rectangle(770,   280, 88, 24);

        private static readonly Point Standard1Button = new Point(184, 390);
        private static readonly Point Heroic1Button = new Point(490, 390);
        private static readonly Point Event1Button = new Point(810, 390);

        private static readonly Point Standard10Button = new Point(184, 460);
        private static readonly Point Heroic10Button = new Point(490, 460);
        private static readonly Point Event10Button = new Point(810, 460);

        private static readonly Point CloseButton = new Point(938, 26);
        public Tavern(GameContext context) : base(context) { }

        protected override Tuple<Point, Color>[] RequiredPoints =>
            new[] {
                Tuple.Create(new Point(75, 467), Color.FromArgb(41, 78, 143)),
                Tuple.Create(new Point(395, 430), Color.FromArgb(162, 105, 46)),
                Tuple.Create(new Point(710, 430), Color.FromArgb(104, 44, 129))
            };

        public long StandardCards => Context.ScaledNumberFromBitmap(CurrentScreen, StandardCardsBox);
        public long HeroicCards => Context.ScaledNumberFromBitmap(CurrentScreen, HeroicCardsBox);
        public long EventPoints => Context.ScaledNumberFromBitmap(CurrentScreen, EventPointsBox);

        public Task PressClaimStandard1Button(CancellationToken cancellationToken) => Context.ClickAt(Standard1Button, cancellationToken);
        public Task PressClaimHeroic1Button(CancellationToken cancellationToken) => Context.ClickAt(Heroic1Button, cancellationToken);
        public Task PressClaimEvent1Button(CancellationToken cancellationToken) => Context.ClickAt(Event1Button, cancellationToken);

        public Task PressClaimStandard10Button(CancellationToken cancellationToken) => Context.ClickAt(Standard10Button, cancellationToken);
        public Task PressClaimHeroic10Button(CancellationToken cancellationToken) => Context.ClickAt(Heroic10Button, cancellationToken);
        public Task PressClaimEvent10Button(CancellationToken cancellationToken) => Context.ClickAt(Event10Button, cancellationToken);

        public Task PressCloseButton(CancellationToken cancellationToken) => Context.ClickAt(CloseButton, cancellationToken);
    }
}
