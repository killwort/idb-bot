using System;
using System.Windows;
using System.Windows.Interop;

namespace IBDTools {
    public class KbHookWindow : Window {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT

        public KbHookWindow() {
            Loaded += (_, __) => {
                var hWnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hWnd)?.AddHook(HwndHook);
                WinApi.RegisterHotKey(hWnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, 0x53);
            };
            Unloaded += (_, __) => {
                var hWnd = new WindowInteropHelper(this).Handle;
                WinApi.UnregisterHotKey(hWnd, HOTKEY_ID);
            };
        }

        protected virtual void Stop() { }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            switch (msg) {
                case WM_HOTKEY:
                    switch (wParam.ToInt32()) {
                        case HOTKEY_ID:
                            var vk = ((int) lParam >> 16) & 0xFFFF;
                            if (vk == 0x53) {
                                Stop();
                            }

                            handled = true;
                            break;
                    }

                    break;
            }

            return IntPtr.Zero;
        }
    }
}