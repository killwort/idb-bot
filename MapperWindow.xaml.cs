using System;
using System.Windows;
using System.Windows.Interop;

namespace IBDTools {


    public partial class MapperWindow : KbHookWindow {
        public MapperWindow() { InitializeComponent(); }

        public VMs.MapperWindow VM => (VMs.MapperWindow) DataContext;
        private void StartBattle(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
