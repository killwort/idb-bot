using System;
using System.Windows;
using System.Windows.Interop;

namespace IBDTools {
    public partial class ArenaWindow : Window {
        public ArenaWindow() { InitializeComponent(); }

        public VMs.ArenaWindow VM => ((VMs.ArenaWindow) DataContext);
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT

        private void ArenaWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle).AddHook(HwndHook);
            WinApi.RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, 0x53);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            const int WM_HOTKEY = 0x0312;
            switch (msg) {
                case WM_HOTKEY:
                    switch (wParam.ToInt32()) {
                        case HOTKEY_ID:
                            int vkey = (((int) lParam >> 16) & 0xFFFF);
                            if (vkey == 0x53) {
                                VM.Stop();
                            }

                            handled = true;
                            break;
                    }

                    break;
            }

            return IntPtr.Zero;
        }

        private void ArenaWindow_OnUnloaded(object sender, RoutedEventArgs e) {
            var handle = new WindowInteropHelper(this).Handle;
            WinApi.UnregisterHotKey(handle, HOTKEY_ID);
        }

        private void StartBattle(object sender, RoutedEventArgs e) { VM.Start(); }
    }
}
