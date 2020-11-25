//#define DEBUG_SAVE_BITMAPS
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Tesseract;

namespace IBDTools {
    public class GameContext {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameContext));
        public GameContext() => InitTesseract();

        public bool Connect() => InitCaptureInterface();

        private static Process _process;

        private bool InitCaptureInterface() {
            Logger.Info("Connecting to game");
            _process = Process.GetProcessesByName("DMW").FirstOrDefault();
            if (_process == null) return false;
            Logger.InfoFormat("Found game process {0}", _process.Id);
            WinApi.SetWindowPos(_process.MainWindowHandle, IntPtr.Zero, 0, 0, 1000, 700, 2);
            Logger.Info("Game window size reset");
            return true;
        }

        private TesseractEngine _englishOcr, _englishPunctOcr, _numbersOcr, _numbersScaledOcr;

        public async Task ClickAt(Point point, CancellationToken cancellationToken, int delay = 200) {
            Logger.DebugFormat("Simulate click at ({0}, {1})", point.X, point.Y);
            await WinApi.SendClickAlt(_process.MainWindowHandle, point.X, point.Y, 50, delay, cancellationToken);
        }

        public async Task ClickAt(int x, int y, CancellationToken cancellationToken, int delay = 200) {
            Logger.DebugFormat("Simulate click at ({0}, {1})", x, y);
            await WinApi.SendClickAlt(_process.MainWindowHandle, x, y, 50, delay, cancellationToken);
        }

        public async Task SendKeyboardString(string str, CancellationToken cancellationToken) {
            foreach (var ch in str) {
                var upper = Char.IsUpper(ch);
                var keyCode = Keyboard.KeyCodes.TryGetValue(upper ? ch : Char.ToUpper(ch), out var k) ? k : Keyboard.DirectXKeyStrokes.DIK_UNKNOWN;
                if (keyCode == Keyboard.DirectXKeyStrokes.DIK_UNKNOWN)
                    throw new InvalidOperationException("Unsendable character " + ch);
                Keyboard.SendKey(keyCode, false, Keyboard.InputType.Keyboard);
                await Task.Delay(10); //Non-cancellable!
                Keyboard.SendKey(keyCode, true, Keyboard.InputType.Keyboard);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void InitTesseract() {
            var tesseractData = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tessdata");
            _englishOcr = new TesseractEngine(tesseractData, "eng", EngineMode.TesseractAndLstm);
            _englishOcr.DefaultPageSegMode = PageSegMode.SingleLine;
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _englishOcr.SetVariable("tessedit_zero_rejection", true);
            _englishOcr.SetVariable("load_freq_dawg", false);
            _englishOcr.SetVariable("load_system_dawg", false);
            _englishPunctOcr = new TesseractEngine(tesseractData, "eng", EngineMode.TesseractAndLstm);
            _englishPunctOcr.DefaultPageSegMode = PageSegMode.SingleLine;
            _englishPunctOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM0123456789/*-+:,.%");
            _englishPunctOcr.SetVariable("tessedit_zero_rejection", true);
            _englishPunctOcr.SetVariable("load_freq_dawg", false);
            _englishPunctOcr.SetVariable("load_system_dawg", false);
            _numbersOcr = new TesseractEngine(tesseractData, "eng", EngineMode.TesseractAndLstm);
            _numbersOcr.DefaultPageSegMode = PageSegMode.SingleLine;
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");
            _numbersOcr.SetVariable("tessedit_zero_rejection", true);
            _numbersOcr.SetVariable("load_freq_dawg", false);
            _numbersOcr.SetVariable("load_system_dawg", false);
            _numbersScaledOcr = new TesseractEngine(tesseractData, "eng", EngineMode.TesseractAndLstm);
            _numbersScaledOcr.DefaultPageSegMode = PageSegMode.SingleLine;
            _numbersScaledOcr.SetVariable("tessedit_char_whitelist", "0123456789.,KMB");
            _numbersScaledOcr.SetVariable("tessedit_zero_rejection", true);
            _numbersScaledOcr.SetVariable("load_freq_dawg", false);
            _numbersScaledOcr.SetVariable("load_system_dawg", false);
            Logger.Info("Tessract engines initialized");
        }

        public string TextFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.ExtractTrim(rt).Normalize())
            using (var page = _englishOcr.Process(subBitmap)) {
                var id = Logger.IsDebugEnabled ? SaveBitmapPart(bm, subBitmap) : "";
                var text = page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from bitmap {5} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, id);
                return text;
            }
        }
        public string TextWithPunctFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.ExtractTrim(rt).Normalize())
            using (var page = _englishPunctOcr.Process(subBitmap)) {
                var id = Logger.IsDebugEnabled ? SaveBitmapPart(bm, subBitmap) : "";
                var text = page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from bitmap {5} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, id);
                return text;
            }
        }

        private static readonly Regex NumbersRegex = new Regex("[^0-9]+", RegexOptions.Compiled);

        public long NumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.ExtractTrim(rt).Normalize())
            using (var page = _numbersOcr.Process(subBitmap)) {
                var text = page.GetText().Trim();
                text = NumbersRegex.Replace(text, "");
                var success = long.TryParse(text, out var rv);
                string bmId = null;
                if (Logger.IsDebugEnabled && (string.IsNullOrEmpty(text) || !success)) bmId = SaveBitmapPart(bm, subBitmap);

                Logger.DebugFormat("Read \"{0}\" as number {5} ({6}) from bitmap {7} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, rv, success ? "successfully" : "failed", bmId);
                return rv;
            }
        }

        private static readonly Regex ScaledNumberRegex = new Regex("[^0-9,.MBK]+", RegexOptions.Compiled);
        private static readonly Regex ScaledNumberParseRegex = new Regex("(?<int>[0-9]+)(,(?<dec>[0-9]+))?(?<exp>[MBK])?", RegexOptions.Compiled);

        public long ScaledNumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.ExtractTrim(rt).Normalize())
            using (var page = _numbersScaledOcr.Process(subBitmap)) {
                var text = page.GetText().Trim();
                if (text.Contains(' ') && !text.Contains(',')) {
                    var i = text.IndexOf(' ');
                    text = text.Remove(i, 1).Insert(i, ",");
                }

                text = ScaledNumberRegex.Replace(text, "");
                var m = ScaledNumberParseRegex.Match(text);
                long n = 0;
                if (m.Success) {
                    var mul = 1;
                    if (m.Groups["exp"].Success)
                        switch (m.Groups["exp"].Value) {
                            case "M":
                                mul = 1000000;
                                break;
                            case "B":
                                mul = 1000000000;
                                break;
                            case "K":
                                mul = 1000;
                                break;
                        }

                    n = long.Parse(m.Groups["int"].Value) * mul + (m.Groups["dec"].Success ? long.Parse(m.Groups["dec"].Value) * mul / (long) Math.Pow(10, m.Groups["dec"].Length) : 0);
                }

                string bmId = null;
                if (Logger.IsDebugEnabled && (string.IsNullOrEmpty(text) )) bmId = SaveBitmapPart(bm, subBitmap);

                Logger.DebugFormat("Read \"{0}\" as number {5} from bitmap {6} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, n, bmId);
                return n;
            }
        }
#if false
        private static Bitmap NormalizeBitmap(Bitmap tbm) {
            var w = tbm.Width;
            var h = tbm.Height;

            var sd = tbm.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bytes = sd.Stride * sd.Height;
            var buffer = new byte[bytes];
            var result = new byte[bytes];
            Marshal.Copy(sd.Scan0, buffer, 0, bytes);
            tbm.UnlockBits(sd);
            var current = 0;
            byte max = 0;
            byte min = 255;
            var histogram = new int[256];

            for (var i = 0; i < buffer.Length; i += 4) {
                var mixed = buffer[i] + buffer[i + 1] + buffer[i + 2];
                mixed /= 3;
                histogram[mixed]++;
            }

            var lowLevel = (w * h * 1 / 100);

            for (var i = 0; i < histogram.Length; i++)
                if (histogram[i] > lowLevel) {
                    min = (byte) (i - 1);
                    break;
                }

            for (var i = histogram.Length - 2; i >= 0; i--)
                if (histogram[i] > lowLevel) {
                    max = (byte) (i + 1);
                    break;
                }

            if (min == max) max++;

            for (var y = 0; y < h; y++) {
                for (var x = 0; x < w; x++) {
                    current = y * sd.Stride + x * 4;
                    var mixed = buffer[current] + buffer[current + 1] + buffer[current + 2];
                    mixed = Math.Min(255, Math.Max(0, mixed / 3 - min) * 255 / (max - min));
                    for (var i = 0; i < 3; i++) {
                        result[current + i] = (byte) mixed; // (byte) ((buffer[current + i] - min) * 100 / (max - min));
                    }

                    result[current + 3] = 255;
                }
            }

            var resimg = new Bitmap(w, h);
            var rd = resimg.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, rd.Scan0, bytes);
            resimg.UnlockBits(rd);
            return resimg;
        }
#endif
#if false
        private static Bitmap NormalizeBitmapOpenCv(Bitmap bm) {
            using (var full = BitmapConverter.ToMat(bm))
            using (var part = new Mat(full, new OpenCvSharp.Rect(rt.X, rt.Y, rt.Width, rt.Height))) {
                using (var gray = part.Clone()) {
                    gray.CvtColor(ColorConversionCodes.RGBA2GRAY);
                    Mat hist = new Mat();
                    int[] hdims = {256}; // Histogram size for each dimension
                    Rangef[] ranges = {new Rangef(0, 256),}; // min/max
                    Cv2.CalcHist(new Mat[] {gray}, new int[] {0}, null, hist, 1, hdims, ranges);
                    // calculate cumulative distribution from the histogram
                    var histSize = 256;
                    float[] accumulator = new float[histSize];
                    accumulator[0] = hist.At<float>(0);
                    for (int i = 1; i < histSize; i++) {
                        accumulator[i] = accumulator[i - 1] + hist.At<float>(i);
                    }

                    float max = accumulator.Last();
                    var clipHistPercent = 5 * (max / 100.0); //make percent as absolute
                    clipHistPercent /= 2.0; // left and right wings
                    // locate left cut
                    var minGray = 0;
                    while (accumulator[minGray] < clipHistPercent)
                        minGray++;

                    // locate right cut
                    var maxGray = histSize - 1;
                    while (accumulator[maxGray] >= (max - clipHistPercent))
                        maxGray--;

                    float inputRange = maxGray - minGray;

                    var alpha = (histSize - 1) / inputRange; // alpha expands current range to histsize range
                    var beta = -minGray * alpha; // beta shifts current range so that minGray will go to 0

                    // Apply brightness and contrast normalization
                    // convertTo operates with saurate_cast
                    using (var dest = new Mat()) {
                        gray.ConvertTo(dest, -1, alpha, beta);

                        return BitmapConverter.ToBitmap(dest);
                    }
                }
            }
        }
#endif

        private static string SaveBitmapPart(Bitmap bm, Bitmap subbitmap) {
#if DEBUG_SAVE_BITMAPS
            var basename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "images");
            if (!Directory.Exists(basename))
                Directory.CreateDirectory(basename);
            var id = Guid.NewGuid().ToString("N");
            subbitmap.Save(Path.Combine(basename, id + $".part.png"), System.Drawing.Imaging.ImageFormat.Png);
            bm.Save(Path.Combine(basename, id + ".full.png"), System.Drawing.Imaging.ImageFormat.Png);
            return id;
#else
            return string.Empty;
#endif
        }

        private static string SaveBitmapPart(Bitmap bm, Rectangle rt) {
#if DEBUG_SAVE_BITMAPS
            var basename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "images");
            if (!Directory.Exists(basename))
                Directory.CreateDirectory(basename);
            var id = Guid.NewGuid().ToString("N");
            bm.Save(Path.Combine(basename, id + ".full.png"), System.Drawing.Imaging.ImageFormat.Png);
            using (var tbm = new Bitmap(rt.Size.Width, rt.Size.Height)) {
                using (var dc = Graphics.FromImage(tbm))
                    dc.DrawImage(bm, new Rectangle(new Point(0, 0), rt.Size), rt, GraphicsUnit.Pixel);
                tbm.Save(Path.Combine(basename, id + $".({rt.X},{rt.Y})+({rt.Width},{rt.Height}).png"), System.Drawing.Imaging.ImageFormat.Png);
            }

            return id;
#else
            return string.Empty;
#endif
        }

        public Bitmap FullScreenshot() => CaptureWindow(_process.MainWindowHandle);

        private static Bitmap CaptureWindow(IntPtr hwnd) {
            WinApi.BringWindowToTop(hwnd);
            WinApi.GetClientRect(hwnd, out var clientRect);
            var pt = new POINT();
            var bm = new Bitmap(clientRect.Width,clientRect.Height);
            WinApi.ClientToScreen(hwnd, ref pt);
            using (var dc = Graphics.FromImage(bm))
                dc.CopyFromScreen(pt.X, pt.Y, 0, 0, clientRect.Size);
            return bm;
        }

    }
}
