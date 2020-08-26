using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Capture;
using Capture.Interface;
using Tesseract;

namespace IBDTools {
    public class GameContext {
        public GameContext() {
            InitTesseract();
        }
        private static CaptureProcess _captureProcess;

        public bool Connect() => InitCaptureInterface();

        private Process _process;
        private bool InitCaptureInterface() {
            _process = Process.GetProcessesByName("DMW").FirstOrDefault();
            if (_process == null) return false;
            WinApi.SetWindowPos(_process.MainWindowHandle, IntPtr.Zero, 0, 0, 1000, 700, 2);
            CaptureConfig cc = new CaptureConfig() {
                Direct3DVersion = Direct3DVersion.AutoDetect,
                ShowOverlay = false
            };
            var captureInterface = new CaptureInterface();
            _captureProcess = new CaptureProcess(_process, cc, captureInterface);
            return true;
        }

        private TesseractEngine _englishOcr, _numbersOcr;

        public void ClickAt(Point point) {
            WinApi.SendClickAlt(_process.MainWindowHandle, point.X, point.Y);
        }
        public void ClickAt(int x, int y) {
            WinApi.SendClickAlt(_process.MainWindowHandle, x, y);
        }

        private void InitTesseract() {
            var tesseractData = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tessdata");
            _englishOcr = new TesseractEngine(tesseractData, "eng");
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _numbersOcr = new TesseractEngine(tesseractData, "eng");
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");
        }

        public string TextFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _englishOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return page.GetText().Trim();
        }

        public string TextFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _englishOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height)))
                return page.GetText().Trim();
        }

        public long NumberFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _numbersOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return long.TryParse(page.GetText().Trim(), out var rv) ? rv : 0;
        }

        public long NumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _numbersOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height)))
                return long.TryParse(page.GetText().Trim(), out var rv) ? rv : 0;
        }

        public Bitmap FullScreenshot() { return GrabWindowPart(Rectangle.Empty); }

        private static Bitmap GrabWindowPart(Rectangle rt) {
            var scrn = _captureProcess.CaptureInterface.GetScreenshot(rt, TimeSpan.FromSeconds(2), null, Capture.Interface.ImageFormat.Bitmap);
            return new Bitmap(new MemoryStream(scrn.Data));
        }
    }
}
