using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using Tesseract;

namespace IBDTools {
    partial class Program {
        private static Process _process;
        private static long _myScore, _myPower, _maxScore = long.MaxValue, _minTickets = 0;
        private static ManualResetEvent _stopEvent;

        public static void Main(string[] args) {
            InitCaptureInterface();
            InitTesseract();
            Console.Write("\nWelcome to the IBD arena bot.\nPlease enter target score [default = unlimited]: ");
            while (true) {
                var l = Console.ReadLine();
                if (!string.IsNullOrEmpty(l)) {
                    if (long.TryParse(l, out _maxScore) && _maxScore > 0) break;
                    Console.Write("Invalid value, try again: ");
                } else break;
            }

            Console.Write("Now enter minimum number of arena tickets to reserve [default 0]: ");
            while (true) {
                var l = Console.ReadLine();
                if (!string.IsNullOrEmpty(l)) {
                    if (long.TryParse(l, out _minTickets) && _minTickets > 0) break;
                    Console.Write("Invalid value, try again: ");
                } else break;
            }

            Console.WriteLine($"All set, now enter main arena screen (ranking) and press enter!\nThe battle will continue until reaching {_minTickets} tickets or {_maxScore} score.");
            Console.ReadLine();

            _stopEvent = new ManualResetEvent(false);
            var thr = new Thread(ArenaBot);
            thr.Start();
            thr.Join();
            Console.SetCursorPosition(0, 5);
        }

        private static void ArenaBot() {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("IBD ARENA BOT");
            while (ProcessArenaMainScreen() && ProcessArenaMatcherScreen() && ProcessArenaBattle()) {
                Console.ForegroundColor = ConsoleColor.Gray;
                if (_stopEvent.WaitOne(0)) return;
            }
        }

        private static bool ProcessArenaBattle() {
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Entering battle                                                     ");
            WinApi.SendClickAlt(_process.MainWindowHandle, 753, 439);
            Thread.Sleep(2000);
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Return to main arena screen                                         ");
            WinApi.SendClickAlt(_process.MainWindowHandle, 788, 121);
            Thread.Sleep(500);
            return true;
        }

        private static bool ProcessArenaMatcherScreen() {
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Choosing opponent                                                   ");
            while (true) {
                if (_stopEvent.WaitOne(0)) return false;
                var fullBitmap = FullScreenshot();
                _myPower = NumberFromBitmap(fullBitmap, new Rectangle(250, 175, 460, 19));
                if (_myPower == 0) {
                    Console.SetCursorPosition(0, 2);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"You're not at the arena matcher screen (or your power is zero), stopping");
                    return false;
                }

                var ticketsLeft = NumberFromBitmap(fullBitmap, new Rectangle(495, 68, 100, 19));
                if (ticketsLeft <= _minTickets) {
                    Console.SetCursorPosition(0, 2);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Minimum tickets reached, stopping                   ");
                    return false;
                }

                var pixColor = fullBitmap.GetPixel(679, 216);
                var fastBattleIsChecked = pixColor.G > 200;
                if (!fastBattleIsChecked)
                    WinApi.SendClickAlt(_process.MainWindowHandle, 679, 216);

                var v1Power = NumberFromBitmap(fullBitmap, new Rectangle(337, 295, 158, 18));
                var v2Power = NumberFromBitmap(fullBitmap, new Rectangle(337, 396, 158, 18));
                var v3Power = NumberFromBitmap(fullBitmap, new Rectangle(337, 499, 158, 18));
                Console.SetCursorPosition(0, 4);
                Console.WriteLine($"Found opponents with powers {Pretty(v1Power)} {Pretty(v2Power)} {Pretty(v3Power)}, my power {Pretty(_myPower)}                     ");
                if ((double) v1Power / _myPower < .9 && v1Power > 0) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 285);
                    return true;
                }

                if ((double) v2Power / _myPower < .9 && v1Power > 0) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 385);
                    return true;
                }

                if ((double) v3Power / _myPower < .9 && v1Power > 0) {
                    WinApi.SendClickAlt(_process.MainWindowHandle, 702, 485);
                    return true;
                }

                Console.SetCursorPosition(0, 1);
                Console.WriteLine("Opponents are too strong, let's reroll...                            ");
                WinApi.SendClickAlt(_process.MainWindowHandle, 723, 181);
            }
        }

        private static bool ProcessArenaMainScreen() {
            var arena = TextFromScreen(new Rectangle(119, 78, 98, 36));
            if (!string.Equals(arena, "arena", StringComparison.InvariantCultureIgnoreCase)) {
                Console.SetCursorPosition(0, 2);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You're not at the main arena screen, stopping                                ");
                return false;
            }

            var score = NumberFromScreen(new Rectangle(720, 333, 120, 33));
            Console.SetCursorPosition(0, 3);
            Console.WriteLine($"My current score is {Pretty(score)}                         ");
            WinApi.SendClickAlt(_process.MainWindowHandle, 737, 394);
            return true;
        }
    }
}
