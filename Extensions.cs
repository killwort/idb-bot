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
