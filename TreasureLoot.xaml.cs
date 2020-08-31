using System.Windows;

namespace IBDTools {
    public partial class TreasureLootWindow : KbHookWindow {
        public TreasureLootWindow() { InitializeComponent(); }

        public VMs.TreasureLootWindow VM => (VMs.TreasureLootWindow) DataContext;
        private void StartBattle(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
