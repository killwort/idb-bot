using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace IBDTools.Screens {
    public class WorldMap : VerifyByPointsScreen {
        public WorldMap(GameContext context) : base(context) { }

        protected override Tuple<Point, Color>[] RequiredPoints =>
            new[] {
                Tuple.Create(new Point(62, 591), Color.FromArgb(0x84ffff)), Tuple.Create(new Point(56, 605), Color.FromArgb(0xd1b199)), Tuple.Create(new Point(172, 591), Color.FromArgb(0xe7e3e4)),
                //Tuple.Create(EnterCoordinatesButton, Color.FromArgb(0x54405c)),
            };

        private static readonly Point EnterCoordinatesButton = new Point(942, 393);
        private static readonly Point EnterCoordinatesGoButton = new Point(860, 110);
        private static readonly Color EnterCoordinatesGoButtonColor = Color.FromArgb(0x825447);
        private static readonly Point EnterCoordinatesX = new Point(740, 110);
        private static readonly Point EnterCoordinatesY = new Point(810, 110);

        public async Task GoTo(int x, int y, CancellationToken cancellationToken) {
            int clicks = 5;
            //Just click somewhere to make selection disappear (it may be rendered over "go to" button)
            await Context.ClickAt(new Point(500, 250), cancellationToken, 50);
            do {
                await Context.ClickAt(EnterCoordinatesButton, cancellationToken, 50);
                CurrentScreen = Context.FullScreenshot();
            } while (!CurrentScreen.VerifyPixelColor(EnterCoordinatesGoButton, EnterCoordinatesGoButtonColor) && --clicks > 0);

            if (clicks <= 0)
                throw new InvalidOperationException("Cannot enter target coordinates. Screen may have changed.");
            await Context.ClickAt(EnterCoordinatesX, cancellationToken, 50);
            await Context.SendKeyboardString(x.ToString(), cancellationToken);
            await Context.ClickAt(EnterCoordinatesY, cancellationToken, 50);
            await Context.SendKeyboardString(y.ToString(), cancellationToken);
            await Context.ClickAt(EnterCoordinatesGoButton, cancellationToken);
        }

        private static readonly Size TileSize = new Size(156, 78);
        private static readonly Point Origin = new Point(491, 407 - TileSize.Height / 2);
        private static readonly Color SelectionBoxPixel1 = Color.FromArgb(0xf6ecaa);
        private static readonly Color SelectionBoxPixel2 = Color.FromArgb(0xf6ecaa);
        private static readonly Color SelectionBoxPixel3 = Color.FromArgb(0xf7efce);

        private static readonly int[] TitleDetectColors = {0x595169, 0x54476c, 0x665a7d, 0x62547c};
        private static readonly int[] SubtitleDetectColors = {0x292430};

        private async Task<Bitmap> GetTileBitmap(int xx, int yy, string saveTo, bool forceCapture, CancellationToken cancellationToken) {
            Bitmap xbm;
            var origin = new Point(Origin.X, Origin.Y);
            origin.Offset(-(xx - yy) * TileSize.Width / 2, (xx + yy) * TileSize.Height / 2);
            if (File.Exists(saveTo + ".png") && !forceCapture) {
                xbm = new Bitmap(saveTo + ".png");
            } else {
                var clicks = 5;
                int pixelQuorum;
                do {
                    await Context.ClickAt(origin, cancellationToken, 100);
                    CurrentScreen = Context.FullScreenshot();
                    var px = new[] {
                        CurrentScreen.GetPixel(origin.X - TileSize.Width / 2 + 6, origin.Y),
                        CurrentScreen.GetPixel(origin.X + TileSize.Width / 2 - 6, origin.Y),
                        CurrentScreen.GetPixel(origin.X, origin.Y + TileSize.Height / 2 - 4)
                    };
                    pixelQuorum = CurrentScreen.VerifyPixelColor(new Point(origin.X - TileSize.Width / 2 + 6, origin.Y), SelectionBoxPixel1, 50) ? 1 : 0;
                    pixelQuorum += CurrentScreen.VerifyPixelColor(new Point(origin.X + TileSize.Width / 2 - 6, origin.Y), SelectionBoxPixel2, 50) ? 1 : 0;
                    pixelQuorum += CurrentScreen.VerifyPixelColor(new Point(origin.X - 6, origin.Y + TileSize.Height / 2 - 4), SelectionBoxPixel3, 50) ? 1 : 0;
                } while (clicks-- > 0 && pixelQuorum < 2);

                if (clicks <= 0) {
                    return null;
                    //throw new InvalidOperationException("Selection box didn't appear.");
                }

                xbm = CurrentScreen.Extract(new Rectangle(origin.X - 278, origin.Y - 187, 389, 294));
                if (saveTo != null)
                    xbm.Save(saveTo + ".png", ImageFormat.Png);
            }

            return xbm;
        }

        private class AutoUnlockedBitmapData : IDisposable {
            public AutoUnlockedBitmapData(Bitmap bmp, Rectangle rt) {
                BitmapData = bmp.LockBits(rt, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                _bitmap = bmp;
            }

            private Bitmap _bitmap;
            public BitmapData BitmapData { get; }
            public string SaveToOnUnlock { get; set; }

            public void Dispose() {
                _bitmap.UnlockBits(BitmapData);
                if (SaveToOnUnlock != null)
                    _bitmap.Save(SaveToOnUnlock, ImageFormat.Png);
            }
        }

        public async Task<TileInfo> SelectTile(int xx, int yy, string saveTo, CancellationToken cancellationToken) {
            bool first = true;
            var titleTries = 2;
            while (titleTries-- > 0) {
                using (var xbm = await GetTileBitmap(xx, yy, saveTo, !first, cancellationToken)) {
                    first = false;
                    Point? titleTop;
                    Point? titleBottom;
                    bool isPlayer;
                    Point? subTitleTop;
                    using (var bmDataU = new AutoUnlockedBitmapData(xbm, new Rectangle(0, 0, xbm.Width, xbm.Height))) {
                        var bmData = bmDataU.BitmapData;
                        //Look for title frame upwards from tile top
                        var titlePoint = bmData.FindColors(new Point(168, 135), new Size(0, -1), TitleDetectColors, 10);
                        if (!titlePoint.HasValue) {
                            await Task.Delay(100, cancellationToken);
                            continue;
                        }

                        //Look for title top and bottom borders

                        titleTop = bmData.FindEdge(new Point(228, titlePoint.Value.Y - 5), new Size(0, -11), 60);
                        titleBottom = bmData.FindEdge(new Point(228, titlePoint.Value.Y + 5), new Size(0, 1), 60);

                        if (!titleBottom.HasValue || !titleTop.HasValue || titleBottom.Value.Y - titleTop.Value.Y > 35 || titleBottom.Value.Y - titleTop.Value.Y < 20) {
                            titleTop = bmData.FindEdge(new Point(212, titlePoint.Value.Y - 5), new Size(0, -11), 60);
                            titleBottom = bmData.FindEdge(new Point(212, titlePoint.Value.Y + 5), new Size(0, 1), 60);
                            if (!titleBottom.HasValue || !titleTop.HasValue || titleBottom.Value.Y - titleTop.Value.Y > 35 || titleBottom.Value.Y - titleTop.Value.Y < 20) {
                                bmDataU.SaveToOnUnlock = saveTo + ".nxtitle.png";
                                return null;
                            }
                        }

                        int sampleLine = (titleBottom.Value.Y - titleTop.Value.Y) * 3 / 4 + titleTop.Value.Y;
                        isPlayer = false;
                        for (int px = 170; px < 210; px++) {
                            var pixColor = Marshal.ReadInt32(bmData.Scan0 + bmData.Stride * sampleLine + px * sizeof(int));
                            Marshal.WriteInt32(bmData.Scan0 + bmData.Stride * (sampleLine + 1) + px * sizeof(int), Color.FromArgb(255, 0, 0, 255).ToArgb());
                            if ((pixColor & 0xff) + ((pixColor >> 8) & 0xff) + ((pixColor >> 16) & 0xff) < 384 && (pixColor & 0xff) < 100 && ((pixColor >> 8) & 0xff) < 100 &&
                                ((pixColor >> 8) & 0xff) < 100) {
                                isPlayer = true;
                                break;
                            }
                        }

                        subTitleTop = bmData.FindColors(new Point(173, titleBottom.Value.Y + 1), new Size(0, 1), SubtitleDetectColors, 30);
                        if (!subTitleTop.HasValue || subTitleTop.Value.Y - titleBottom.Value.Y > 30) subTitleTop = new Point(0, titleBottom.Value.Y + 7);

                    }

                    //We actually have found title section
                    if (titleBottom.Value.Y > 125) {
                        //It's scenery tile
                        return new SceneryTileInfo { };
                    } else if (titleBottom.Value.Y > 105) {
                        //It's player castle or monster
                        if (isPlayer)
                            return new PlayerCastleTileInfo {
                                Name = Context.TextFromBitmap(xbm, new Rectangle(240, titleTop.Value.Y, 145, titleBottom.Value.Y - titleTop.Value.Y)),
                                Guild= Context.TextFromBitmap(xbm, new Rectangle(64, subTitleTop.Value.Y+34, 95,23)),
                            };
                        else
                            return new MonsterTileInfo { };
                    } else if (titleBottom.Value.Y > 75) {
                        //It's capturable town/honor town
                        return new NpcCastleTileInfo {
                            Name = Context.TextFromBitmap(xbm, new Rectangle(240, titleTop.Value.Y + 5, 120, titleBottom.Value.Y - titleTop.Value.Y - 10)),
                            Level = (int) Context.NumberFromBitmap(xbm, new Rectangle(210, titleTop.Value.Y + 5, 20, titleBottom.Value.Y - titleTop.Value.Y - 10)),
                            Defence = (int) Context.NumberFromBitmap(xbm, new Rectangle(133, subTitleTop.Value.Y + 5, 20, 20)),
                            Owner = Context.TextFromBitmap(xbm, new Rectangle(3, subTitleTop.Value.Y + 38, 158, 20)),
                            Bonus1 = Context.TextFromBitmap(xbm, new Rectangle(3, subTitleTop.Value.Y + 70, 158, xbm.Height-subTitleTop.Value.Y-70))
                        };

                    } else if (titleBottom.Value.Y > 65) {
                        //It's a crypt
                        return new SceneryTileInfo();
                    } else if (titleBottom.Value.Y > 33) {
                        //It's a guild boss
                        return new SceneryTileInfo();
                    } else {
                        if (saveTo != null) {
                            xbm.Save(saveTo+".softfail.png",ImageFormat.Png);
                        }
                        return null;
                    }

                }
            }

            //Let the "Coming soon" messages scroll away!
            await Task.Delay(500, cancellationToken);
            return null;
        }
    }

    public class TileInfo {
        public Point CenterPoint;
        //public int X, Y;
    }

    public class SceneryTileInfo : TileInfo {
    }

    public class MonsterTileInfo : SceneryTileInfo {
    }

    public class PlayerCastleTileInfo : TileInfo {
        public string Name, Guild;
    }

    public class NpcCastleTileInfo : TileInfo {
        public string Name;
        public int Level;
        public string Owner;
        public int Defence;
        public string Bonus1;
        public string Bonus2;
    }
}
