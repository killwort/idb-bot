using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Capture;
using Capture.Interface;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace IBDTools {
    internal class Program {
        private static CaptureProcess _captureProcess;
        private static Process _process;
        private static TesseractEngine _englishOcr, _numbersOcr;

        public static void Main(string[] args) {
            InitCaptureInterface();
            var tesseractData = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tessdata");
            _englishOcr = new TesseractEngine(tesseractData, "eng");
            _englishOcr.SetVariable("tessedit_char_whitelist", "qwertyuiopasdfghjklzxcvbnm QWERTYUIOPASDFGHJKLZXCVBNM");
            _numbersOcr = new TesseractEngine(tesseractData, "eng");
            _numbersOcr.SetVariable("tessedit_char_whitelist", "0123456789");

            _stopEvent = new ManualResetEvent(false);
            var thr = new Thread(
                () => {
                    while (ProcessArenaMainScreen() && ProcessArenaMatcherScreen() && ProcessArenaBattle()) {
                        if (_stopEvent.WaitOne(0)) return;
                    }
                }
            );
            thr.Start();
            thr.Join();
        }

        private static bool ProcessArenaBattle() {
            Console.WriteLine("Entering battle");
            WinApi.SendClickAlt(_process.MainWindowHandle, 753, 439);
            Thread.Sleep(2000);
            Console.WriteLine("Return to main arena screen");
            WinApi.SendClickAlt(_process.MainWindowHandle, 788, 121);
            Thread.Sleep(500);
            return true;
        }

        private static bool ProcessArenaMatcherScreen() {
            Console.WriteLine("Choosing opponent");
            while (true) {
                if (_stopEvent.WaitOne(0)) return false;
                _myPower = NumberFromScreen(new Rectangle(250, 175, 460, 19));
                if (_myPower == 0) {
                    Console.WriteLine($"You're not at the arena matcher screen (or your power is zero), stopping");
                    return false;
                }

                var v1Power = NumberFromScreen(new Rectangle(337, 295, 158, 18));
                var v2Power = NumberFromScreen(new Rectangle(337, 396, 158, 18));
                var v3Power = NumberFromScreen(new Rectangle(337, 499, 158, 18));
                Console.WriteLine($"Found opponents with powers {v1Power} {v2Power} {v3Power}, my power {_myPower}");
                if ((double) v1Power / _myPower < .9) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 285);
                    return true;
                }

                if ((double) v2Power / _myPower < .9) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 385);
                    return true;
                }

                if ((double) v3Power / _myPower < .9) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 485);
                    return true;
                }

                Console.WriteLine("Opponents are too strong, let's reroll...");
                WinApi.SendClickAlt(_process.MainWindowHandle, 723, 181);
            }
        }

        private static long _myScore, _myPower;
        private static ManualResetEvent _stopEvent;

        private static bool ProcessArenaMainScreen() {
            var arena = TextFromScreen(new Rectangle(119, 78, 98, 36));
            if (!string.Equals(arena, "arena", StringComparison.InvariantCultureIgnoreCase)) {
                Console.WriteLine("You're not at the main arena screen, stopping");
                return false;
            }

            _myScore = NumberFromScreen(new Rectangle(720, 333, 120, 33));
            Console.WriteLine($"My current score is {_myScore}");
            WinApi.SendClickAlt(_process.MainWindowHandle, 737, 394);
            return true;
        }

        private static string TextFromScreen(Rectangle rt) {
            var bm = GrabWindowPart(rt);
            //bm.Save("d:\\last.png", ImageFormat.Png);
            using (var page = _englishOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return page.GetText().Trim();
        }

        private static long NumberFromScreen(Rectangle rt) {
            var bm = GrabWindowPart(rt);
            //bm.Save("d:\\last.png", ImageFormat.Png);
            using (var page = _numbersOcr.Process(bm, new Rect(0, 0, rt.Width, rt.Height)))
                return long.TryParse(page.GetText().Trim(), out var rv) ? rv : 0;
        }

        private static Bitmap GrabWindowPart(Rectangle rt) {
            var scrn = _captureProcess.CaptureInterface.GetScreenshot(rt, TimeSpan.FromSeconds(2), null, Capture.Interface.ImageFormat.Bitmap);
            var h = scrn.Height;
            var w = scrn.Width;
            return new Bitmap(new MemoryStream(scrn.Data));
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
    }
}
