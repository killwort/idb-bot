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
    partial class Program {
        private static CaptureProcess _captureProcess;

        private static string TextFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _englishOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return page.GetText().Trim();
        }

        private static string TextFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _englishOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height)))
                return page.GetText().Trim();
        }

        private static long NumberFromScreen(Rectangle rt) {
            using (var bm = GrabWindowPart(rt))
            using (var page = _numbersOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return long.TryParse(page.GetText().Trim(), out var rv) ? rv : 0;
        }

        private static long NumberFromBitmap(Bitmap bm, Rectangle rt) {
            using (var page = _numbersOcr.Process(bm, new Rect(rt.Left, rt.Top, rt.Width, rt.Height)))
                return long.TryParse(page.GetText().Trim(), out var rv) ? rv : 0;
        }

        private static Bitmap FullScreenshot() { return GrabWindowPart(Rectangle.Empty); }

        private static Bitmap GrabWindowPart(Rectangle rt) {
            var scrn = _captureProcess.CaptureInterface.GetScreenshot(rt, TimeSpan.FromSeconds(2), null, Capture.Interface.ImageFormat.Bitmap);
            return new Bitmap(new MemoryStream(scrn.Data));
        }

        private static string Pretty(long n) {
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

        private static void InitCaptureInterface() {
            _process = Process.GetProcessesByName("DMW").First();
            WinApi.SetWindowPos(_process.MainWindowHandle, IntPtr.Zero, 0, 0, 1000, 700, 2);
            CaptureConfig cc = new CaptureConfig() {
                Direct3DVersion = Direct3DVersion.AutoDetect,
                ShowOverlay = false
            };
            var captureInterface = new CaptureInterface();
            _captureProcess = new CaptureProcess(_process, cc, captureInterface);
        }

        private static TesseractEngine _englishOcr, _numbersOcr;

        private static void InitTesseract() {
            var tesseractData = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tessdata");
            _englishOcr = new TesseractEngine(tesseractData, "eng");
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _numbersOcr = new TesseractEngine(tesseractData, "eng");
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");
        }
    }
}
