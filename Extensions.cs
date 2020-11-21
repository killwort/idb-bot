using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;

namespace IBDTools {
    public static class Extensions {
        public static async Task Activation(this IScreen screen, CancellationToken cancellationToken) {
            while (!screen.IsScreenActive()) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(250, cancellationToken);
            }
        }

        public static bool VerifyPixelColor(this Bitmap bitmap, Point point, Color color, int maxDivergence = 30) {
            var cl = bitmap.GetPixel(point.X, point.Y);
            return (Math.Abs(cl.B - color.B) + Math.Abs(cl.R - color.R) + Math.Abs(cl.G - color.G)) < maxDivergence;
        }

        public static Point? FindEdge(this BitmapData bmData, Point origin, Size delta, int threshold) {
            var current = new Point(origin.X, origin.Y);
            int prevColor = -1;
            var ix = delta.Width == 0 ? 1 : 0;
            var iy = ix == 0 ? 1 : 0;
            while (current.X >= 0 && current.Y >= 0 && current.X < bmData.Width && current.Y < bmData.Height) {
                var pixColor = Marshal.ReadInt32(bmData.Scan0 + bmData.Stride * current.Y + current.X * sizeof(int));
                Marshal.WriteInt32(bmData.Scan0 + bmData.Stride * (iy + current.Y) + (ix + current.X) * sizeof(int), (int) ((~pixColor) | (0xff000000)));
                if (prevColor != -1) {
                    if (pixColor.ColorDiff(prevColor) > threshold) return current;
                }

                prevColor = pixColor;
                current.Offset(Math.Sign(delta.Width), Math.Sign(delta.Height));
            }

            return null;
        }

        public static Point? FindColors(this BitmapData bmData, Point origin, Size delta, IEnumerable<int> matchColors, int threshold) {
            var current = new Point(origin.X, origin.Y);
            var ix = delta.Width == 0 ? 1 : 0;
            var iy = ix == 0 ? 1 : 0;
            while (current.X >= 0 && current.Y >= 0 && current.X < bmData.Width && current.Y < bmData.Height) {
                var pixColor = Marshal.ReadInt32(bmData.Scan0 + bmData.Stride * current.Y + current.X * sizeof(int));
                Marshal.WriteInt32(bmData.Scan0 + bmData.Stride * (iy + current.Y) + (ix + current.X) * sizeof(int), (int) ((~pixColor) | (0xff000000)));
                if (matchColors.Any(x => pixColor.ColorDiff(x) < threshold)) return current;
                current.Offset(Math.Sign(delta.Width), Math.Sign(delta.Height));
            }

            return null;
        }

        public static int ColorDiff(this Color c1, Color c2) { return (Math.Abs(c1.B - c2.B) + Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G)); }

        public static int ColorDiff(this int c1, int c2) {
            return Math.Abs(((c1 >> 8) & 0xff) - ((c2 >> 8) & 0xff)) + Math.Abs(((c1 >> 16) & 0xff) - ((c2 >> 16) & 0xff)) + Math.Abs(((c1) & 0xff) - ((c2) & 0xff));
        }

        public static Bitmap Extract(this Bitmap bm, Rectangle rt) {
            var tbm = new Bitmap(rt.Size.Width, rt.Size.Height);
            using (var dc = Graphics.FromImage(tbm)) {
                dc.DrawImage(bm, new Rectangle(new Point(0, 0), rt.Size), rt, GraphicsUnit.Pixel);
            }

            return tbm;
        }

        public static Bitmap ExtractTrim(this Bitmap bm, Rectangle rt, int colorTolerance = 30) {
            var bmData = bm.LockBits(rt, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var trimRect = new Rectangle(0, 0, bmData.Width, bmData.Height);
            for (var y = 1; y < bmData.Height; y++)
            for (var x = 0; x < bmData.Width; x++) {
                if (ColorDiff(Marshal.ReadInt32(bmData.Scan0 + (y - 1) * bmData.Stride + sizeof(int) * x), Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * x)) > colorTolerance) {
                    trimRect.Y = y - 1;
                    trimRect.Height -= trimRect.Y;
                    goto br0;
                }
            }

            br0:

            for (var y = bmData.Height - 2; y > trimRect.Y; y--)
            for (var x = 0; x < bmData.Width; x++) {
                if (ColorDiff(Marshal.ReadInt32(bmData.Scan0 + (y - 1) * bmData.Stride + sizeof(int) * x), Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * x)) > colorTolerance) {
                    trimRect.Height = y - trimRect.Y + 2;
                    goto br1;
                }
            }

            br1:

            for (var x = 1; x < bmData.Width; x++)
            for (var y = 0; y < bmData.Height; y++) {
                if (ColorDiff(Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * (x - 1)), Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * x)) > colorTolerance) {
                    trimRect.X = x - 1;
                    trimRect.Width -= trimRect.X;
                    goto br2;
                }
            }

            br2:

            for (var x = bmData.Width - 2; x > trimRect.X; x--)
            for (var y = 0; y < bmData.Height; y++) {
                if (ColorDiff(Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * (x - 1)), Marshal.ReadInt32(bmData.Scan0 + y * bmData.Stride + sizeof(int) * x)) > colorTolerance) {
                    trimRect.Width = x - trimRect.X + 2;
                    goto br3;
                }
            }

            br3:

            bm.UnlockBits(bmData);
            var tbm = new Bitmap(trimRect.Size.Width, trimRect.Size.Height);
            using (var dc = Graphics.FromImage(tbm)) {
                rt.Offset(trimRect.Location);
                rt.Size = trimRect.Size;
                dc.DrawImage(bm, new Rectangle(0, 0, trimRect.Width, trimRect.Height), rt, GraphicsUnit.Pixel);
            }

            return tbm;
        }

        public static string Pretty(this long n) {
            var result = "";
            if (n < 0) {
                n = -n;
                result += "-";
            }

            if (n > 1_000_000_000)
                result += ((double) n / 1_000_000_000).ToString("0.##") + "B";
            else if (n > 1_000_000)
                result += ((double) n / 1_000_000).ToString("0.##") + "M";
            else if (n > 1_000)
                result += ((double) n / 1_000).ToString("0.##") + "K";
            else result += n.ToString();
            return result;
        }


        public static Bitmap Normalize(this Bitmap bm) {

            int w = bm.Width;
            int h = bm.Height;

            var bmData = bm.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            byte max = 0;
            byte min = 255;
            for (int i = 0; i < bmData.Stride*bmData.Height; i+=sizeof(int)) {
                var rgb = (Marshal.ReadInt32(bmData.Scan0 + i));
                var colorBytes = BitConverter.GetBytes(rgb);
                var avg = (byte) (colorBytes.Take(3).Sum(x => (int) x) / 3);
                if (max < avg) max = avg;
                if (min > avg) min = avg;
            }

            for (int i = 0; i < bmData.Stride * bmData.Height; i += sizeof(int)) {
                var rgb = (Marshal.ReadInt32(bmData.Scan0 + i));
                var colorBytes = BitConverter.GetBytes(rgb);
                for (var c = 0; c < 3; c++) {
                    colorBytes[c] = (byte) (255 - Math.Min(255, Math.Max(0, (int) colorBytes[c] - min) * 255 / (max - min)));
                }

                Marshal.WriteInt32(bmData.Scan0 + i, BitConverter.ToInt32(colorBytes, 0));
            }
            bm.UnlockBits(bmData);
            return bm;
        }

        public static int DamerauLevenstein(string s1, string s2) {
            if (s1 == s2)
                return 0;
            var s1Length = s1.Length;
            var s2Length = s2.Length;
            if (s1Length == 0 || s2Length == 0)
                return s1Length == 0 ? s2Length : s1Length;

            var matrix = new int[s1Length + 1, s2Length + 1];

            for (var i = 1; i <= s1Length; i++) {
                matrix[i, 0] = i;
                for (var j = 1; j <= s2Length; j++) {
                    var cost = s2[j - 1] == s1[i - 1] ? 0 : 1;
                    if (i == 1)
                        matrix[0, j] = j;

                    var vals = new[] {matrix[i - 1, j] + 1, matrix[i, j - 1] + 1, matrix[i - 1, j - 1] + cost};
                    matrix[i, j] = vals.Min();
                    if (i > 1 && j > 1 && s1[i - 1] == s2[j - 2] && s1[i - 2] == s2[j - 1])
                        matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
                }
            }

            return matrix[s1Length, s2Length];
        }
    }
}
