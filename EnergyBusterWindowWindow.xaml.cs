using System.Windows;

namespace IBDTools {
    public partial class EnergyBusterWindow : KbHookWindow {
        public EnergyBusterWindow() { InitializeComponent(); }

        public VMs.EnergyBusterWindow VM => (VMs.EnergyBusterWindow) DataContext;
        private void StartBattle(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
