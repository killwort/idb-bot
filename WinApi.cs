using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools {
    public static class WinApi {
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        public static void SendClick(IntPtr hWnd, int x, int y) {
            var lparam = (x & 0xFFFF) | ((y & 0xFFFF) << 16);
            var wparam = 0;
            PostMessage(hWnd, 0x201, wparam, lparam);
            Thread.Sleep(10);
            PostMessage(hWnd, 0x202, wparam, lparam);
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public static async Task SendClickAlt(IntPtr hWnd,
                                              int PositionX,
                                              int PositionY,
                                              int delayBetweenClicks,
                                              int delayAfterClick,
                                              CancellationToken cancellationToken) {
            BringWindowToTop(hWnd);
            var pt = new POINT();
            ClientToScreen(hWnd, ref pt);
            PositionX += pt.X;
            PositionY += pt.Y;
            SetCursorPos(PositionX, PositionY);
            mouse_event(MOUSEEVENTF_LEFTDOWN, PositionX, PositionY, 0, 0);
            await Task.Delay(delayBetweenClicks); //Non-cancellable!
            mouse_event(MOUSEEVENTF_LEFTUP, PositionX, PositionY, 0, 0);
            if (delayAfterClick > 0)
                await Task.Delay(delayAfterClick, cancellationToken);
        }

        public static void MoveCursor(IntPtr hWnd, int PositionX, int PositionY) {
            BringWindowToTop(hWnd);
            var pt = new POINT();
            ClientToScreen(hWnd, ref pt);
            PositionX += pt.X;
            PositionY += pt.Y;
            SetCursorPos(PositionX, PositionY);
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd,
                                               IntPtr hWndInsertAfter,
                                               int X,
                                               int Y,
                                               int cx,
                                               int cy,
                                               uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject,
                                         int nXDest,
                                         int nYDest,
                                         int nWidth,
                                         int nHeight,
                                         IntPtr hObjectSource,
                                         int nXSrc,
                                         int nYSrc,
                                         int dwRop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        public static Bitmap PrintWindow(IntPtr hwnd) {
            RECT rc;
            GetWindowRect(hwnd, out rc);

            var bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            var gfxBmp = Graphics.FromImage(bmp);
            var hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }
    }
}
