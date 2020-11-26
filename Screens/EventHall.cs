using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Image = System.Drawing.Image;

namespace IBDTools.Screens {


    public class EventHall : VerifyByPointsScreen {
        private static Bitmap EmptyMap;
        private static readonly Point FindMoreButton = new Point(490, 590);
        private static readonly Point FindMoreDialogButton = new Point(330, 435);

        public async Task ToggleFastBattle(bool value, CancellationToken cancellationToken) {
            if (IsFastBattleEnabled != value) {
                await Context.ClickAt(26, 498, cancellationToken, 0);
                CurrentScreen = Context.FullScreenshot();
            }
        }
        public bool IsFastBattleEnabled {
            get {
                var px=CurrentScreen.GetPixel(26, 498);
                return 0x35e446.ColorDiff(px.ToArgb()) < 32;
            }
        }
        public async Task FindMoreEvents(CancellationToken cancellationToken) {
            await Context.ClickAt(FindMoreButton, cancellationToken);
            await Task.Delay(300, cancellationToken);
            CurrentScreen.Dispose();
            CurrentScreen = Context.FullScreenshot();
            var pix = CurrentScreen.GetPixel(FindMoreDialogButton.X, FindMoreDialogButton.Y).ToArgb();
            if (0xa26852.ColorDiff(pix) < 32) {
                await Context.ClickAt(FindMoreDialogButton, cancellationToken);
                await Task.Delay(300, cancellationToken);
            }
        }

        public EventHall(GameContext context) : base(context) {
            if (EmptyMap == null) {
                EmptyMap = (Bitmap) Image.FromFile(Path.Combine(Path.GetDirectoryName(typeof(EventHall).Assembly.Location), "eventhall.png"));
            }
        }

        protected override Tuple<Point, Color>[] RequiredPoints =>
            new[] {Tuple.Create(new Point(101, 5), Color.FromArgb(0x4c4250)), Tuple.Create(new Point(978, 46), Color.FromArgb(0x94714a)), Tuple.Create(new Point(18, 606), Color.FromArgb(0x9d8664))};

        public class BoundingBox {
            public int Top, Left, Bottom, Right;
            //public int Width, Height;

            public BoundingBox(int x, int y) {
                Top = Bottom = y;
                Left = Right = x;
                //Width = Height = 1;
            }

            public int Height => Bottom - Top + 1;
            public int Width => Right - Left + 1;

            public bool Contains(int x, int y) => y >= Top && y <= Bottom && x >= Left && x <= Right;

            public void Consume(int x, int y) {
                if (x < Left) Left = x;
                else if (x > Right) Right = x;
                if (y < Top) Top = y;
                else if (y > Bottom) Bottom = y;
            }

            public bool Intersects(BoundingBox other) => !(other.Left > Right + 1 || other.Right < Left - 1 || other.Top > Bottom + 1 || other.Bottom < Top - 1);

            public void Consume(BoundingBox other) {
                Top = Math.Min(Top, other.Top);
                Left = Math.Min(Left, other.Left);
                Bottom = Math.Max(Bottom, other.Bottom);
                Right = Math.Max(Right, other.Right);
            }

            public bool Contains(BoundingBox other) => Contains(other.Left, other.Top) && Contains(other.Right, other.Bottom);
            public Rectangle ToRectangle() => new(Left, Top, Width, Height);
        }

        public Event[] Events {
            get {
                Context.MoveCursor(1, 1);
                var emptyData = EmptyMap.LockBits(new Rectangle(0, 0, EmptyMap.Width, EmptyMap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var screenData = CurrentScreen.LockBits(new Rectangle(0, 0, EmptyMap.Width, EmptyMap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var blobBoxes = new List<BoundingBox>();
                try {
                    var diff = new bool[EmptyMap.Width, EmptyMap.Height];
                    for (var i = 0; i < emptyData.Stride * emptyData.Height; i += sizeof(int)) {
                        var emptyC = Marshal.ReadInt32(emptyData.Scan0 + i);
                        if ((emptyC >> 24) == 0) continue;
                        var screenC = Marshal.ReadInt32(screenData.Scan0 + i);
                        var x = i % emptyData.Stride / sizeof(int);
                        var y = i / emptyData.Stride;
                        var isDiff = diff[x, y] = screenC.ColorDiff(emptyC) > 3;
                        if (isDiff) {
                            BoundingBox theBox = null;
                            if (x > 0 && diff[x - 1, y])
                                theBox = blobBoxes.FirstOrDefault(b => b.Contains(x - 1, y));
                            else if (x > 0 && y > 0 && diff[x - 1, y - 1])
                                theBox = blobBoxes.FirstOrDefault(b => b.Contains(x - 1, y - 1));
                            else if (y > 0 && diff[x, y - 1])
                                theBox = blobBoxes.FirstOrDefault(b => b.Contains(x, y - 1));
                            else if (x < emptyData.Width && y > 0 && diff[x + 1, y - 1])
                                theBox = blobBoxes.FirstOrDefault(b => b.Contains(x + 1, y - 1));
                            if (theBox == null)
                                blobBoxes.Add(new BoundingBox(x, y));
                            else
                                theBox.Consume(x, y);
                        }
                    }
                } finally {
                    CurrentScreen.UnlockBits(screenData);
                    EmptyMap.UnlockBits(emptyData);
                }

                bool merges;
                do {
                    merges = false;
                    for (var i1 = 0; i1 < blobBoxes.Count - 1; i1++)
                    for (var i2 = i1 + 1; i2 < blobBoxes.Count; i2++) {
                        if (blobBoxes[i1].Contains(blobBoxes[i2])) {
                            blobBoxes.RemoveAt(i2);
                            i2--;
                            merges = true;
                        } else if (blobBoxes[i1].Intersects(blobBoxes[i2])) {
                            blobBoxes[i1].Consume(blobBoxes[i2]);
                            blobBoxes.RemoveAt(i2);
                            i2--;
                            merges = true;
                        }
                    }
                } while (merges);

                return blobBoxes.Where(x => x.Width > 70 && x.Height > 50 && x.Width < 150).Select(x => ToEvent(x)).ToArray();
            }
        }

        public Task ResolveEvent(Event ev, CancellationToken token) => ev.ResolveEvent(this, this.Context, token);

        private Event ToEvent(BoundingBox boundingBox) {
            //Fix for events close to the right part of the screen (clipped)
            if (boundingBox.Right == 875 || boundingBox.Right == 876)
                boundingBox.Right = boundingBox.Left + 100;
            if (boundingBox.Bottom == 474 || boundingBox.Bottom == 474)
                boundingBox.Bottom = boundingBox.Top + 85;
            var rt = boundingBox.ToRectangle();
            var cPoints = new int[9];
            var i = 0;
            for (var y = rt.Height / 4; y < rt.Height - 4; y += rt.Height / 4)
            for (var x = rt.Width / 4; x < rt.Width - 4; x += rt.Width / 4)
                cPoints[i++] = CurrentScreen.GetPixel(rt.Left + x, rt.Top + y).ToArgb();

            if (BossEvent.ColorPoints.Any(x=>MatchPoints(cPoints,x)))
                return new BossEvent(rt);
            if (ChestEvent.ColorPoints.Any(x=>MatchPoints(cPoints,x)))
                return new ChestEvent(rt);
            if (MatchPoints(cPoints, BarterEvent.ColorPoints,7))
                return new BarterEvent(rt);
            if (ShopEvent.ColorPoints.Any(x=>MatchPoints(cPoints,x)))
                return new ShopEvent(rt);
            if (MatchPoints(cPoints, ExchangeEvent.ColorPoints))
                return new ExchangeEvent(rt);
            if (EscortEvent.ColorPoints.Any(x=>MatchPoints(cPoints,x)))
                return new EscortEvent(rt);

            using (var sbm = new Bitmap(boundingBox.Width, boundingBox.Height)) {
                using (var dc = Graphics.FromImage(sbm)) {
                    dc.DrawImage(CurrentScreen, 0, 0, rt, GraphicsUnit.Pixel);
                }

                var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", $"unknown-event-{string.Join("-", cPoints.Select(x => x.ToString("x8")))}.bmp");

                sbm.Save(file);

            }
            return new UnknownEvent(rt);
        }

        private bool MatchPoints(int[] imagePoints, uint[] refPoints, int consensus = -1) {
            int matches = 0, ignores = 0;
            for (var i = 0; i < 9; i++) {
                if ((refPoints[i] >> 24) == 0xff)
                    ignores++;
                else if (imagePoints[i].ColorDiff((int) (refPoints[i] & 0xffffff)) < (refPoints[i] >> 24) * 3)
                    matches++;
            }

            if (consensus == -1) consensus = Math.Max(1, 8 - ignores);
            return matches >= consensus;
        }
    }
}
