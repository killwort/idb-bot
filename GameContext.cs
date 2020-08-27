using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Capture;
using Capture.Interface;
using log4net;
using Tesseract;

namespace IBDTools {
    public class GameContext {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameContext));
        public GameContext() {
            InitTesseract();
        }
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
            CaptureConfig cc = new CaptureConfig() {
                Direct3DVersion = Direct3DVersion.AutoDetect,
                ShowOverlay = false
            };
            var captureInterface = new CaptureInterface();
            Logger.Info("Capture interface initialized");
            _captureProcess = new CaptureProcess(_process, cc, captureInterface);
            Logger.Info("Process hooked, ready to get screenshots");
            return true;
        }

        private TesseractEngine _englishOcr, _numbersOcr;

        public void ClickAt(Point point) {
            Logger.DebugFormat("Simulate click at ({0}, {1})", point.X, point.Y);
            WinApi.SendClickAlt(_process.MainWindowHandle, point.X, point.Y);
        }
        public void ClickAt(int x, int y) {
            Logger.DebugFormat("Simulate click at ({0}, {1})", x, y);
            WinApi.SendClickAlt(_process.MainWindowHandle, x, y);
        }

        private void InitTesseract() {
            var tesseractData = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tessdata");
            _englishOcr = new TesseractEngine(tesseractData, "eng");
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _numbersOcr = new TesseractEngine(tesseractData, "eng");
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");
            Logger.Info("Tessract engines initialized");
        }

        public string TextFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _englishOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height))) {
                var text=page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from screen rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height);
                return text;
            }
        }

        public string TextFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _englishOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height))) {
                var text=page.GetText().Trim();
                Logger.DebugFormat("Read \"{0}\" from bitmap rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height);
                return text;
            }
        }

        public long NumberFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _numbersOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height))) {
                var text = page.GetText().Trim();
                bool success=long.TryParse(page.GetText().Trim(), out var rv);
                Logger.DebugFormat("Read \"{0}\" as number {5} ({6}) from screen rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, rv, success ? "successfully" : "failed");
                return rv;
            }
        }

        public long NumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _numbersOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height))) {
                var text = page.GetText().Trim();
                bool success=long.TryParse(page.GetText().Trim(), out var rv);
                Logger.DebugFormat("Read \"{0}\" as number {5} ({6}) from bitmap rect ({1}, {2})+({3}, {4})", text, rt.X, rt.Y, rt.Width, rt.Height, rv, success ? "successfully" : "failed");
                return rv;
            }
        }

        public Bitmap FullScreenshot() { return GrabWindowPart(Rectangle.Empty); }

        private static Bitmap GrabWindowPart(Rectangle rt) {
            var scrn = _captureProcess.CaptureInterface.GetScreenshot(rt, TimeSpan.FromSeconds(2), null, Capture.Interface.ImageFormat.Bitmap);
            return new Bitmap(new MemoryStream(scrn.Data));
        }
    }
}
