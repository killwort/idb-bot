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
using Capture;
using Capture.Interface;
using log4net;
using Tesseract;
using ImageFormat = Capture.Interface.ImageFormat;

namespace IBDTools {
    public class GameContext {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameContext));
        public GameContext() => InitTesseract();
        private static CaptureProcess _captureProcess;

        public bool Connect() => InitCaptureInterface();

        private Process _process;

        private bool InitCaptureInterface() {
            Logger.Info("Connecting to game");
            _process = Process.GetProcessesByName("DMW").FirstOrDefault();
            if (_process == null) return false;
            Logger.InfoFormat("Found game process {0}", _process.Id);
            WinApi.SetWindowPos(_process.MainWindowHandle, IntPtr.Zero, 0, 0, 1000, 700, 2);
            Logger.Info("Game window size reset");
            var cc = new CaptureConfig {
                Direct3DVersion = Direct3DVersion.AutoDetect,
                ShowOverlay = false
            };
            var captureInterface = new CaptureInterface();
            Logger.Info("Capture interface initialized");
            _captureProcess = new CaptureProcess(_process, cc, captureInterface);
            Logger.Info("Process hooked, ready to get screenshots");
            return true;
        }

        private TesseractEngine _englishOcr, _englishPunctOcr, _numbersOcr;

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
            _englishOcr = new TesseractEngine(tesseractData, "eng", EngineMode.LstmOnly);
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _englishPunctOcr = new TesseractEngine(tesseractData, "eng", EngineMode.LstmOnly);
            _englishPunctOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM0123456789/*-+:,.%");
            _numbersOcr = new TesseractEngine(tesseractData, "eng", EngineMode.LstmOnly);
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");
            Logger.Info("Tessract engines initialized");
        }

        public string TextFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.Extract(rt))
            using (var page = _englishOcr.Process(subBitmap)) {
                var id = Logger.IsDebugEnabled ? SaveBitmapPart(bm, subBitmap) : "";
                var text = page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from bitmap {5} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, id);
                return text;
            }
        }
        public string TextWithPunctFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.Extract(rt))
            using (var page = _englishPunctOcr.Process(subBitmap)) {
                var id = Logger.IsDebugEnabled ? SaveBitmapPart(bm, subBitmap) : "";
                var text = page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from bitmap {5} rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, id);
                return text;
            }
        }

        private static readonly Regex NumbersRegex = new Regex("[^0-9]+", RegexOptions.Compiled);

        public long NumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var subBitmap = bm.Extract(rt))
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

        public Bitmap FullScreenshot() => GrabWindowPart(Rectangle.Empty);

        private static Bitmap GrabWindowPart(Rectangle rt) {
            var scrn = _captureProcess.CaptureInterface.GetScreenshot(rt, TimeSpan.FromSeconds(2), null, ImageFormat.Bitmap);
            if (scrn.Data == null) {
                Logger.Fatal("Error fetching screenshot data");
                return new Bitmap(1, 1);
            }

            return new Bitmap(new MemoryStream(scrn.Data));
        }
    }
}
