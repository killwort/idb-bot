using System.Windows;

namespace IBDTools {
    public partial class CardsClaimWindow : KbHookWindow {
        public CardsClaimWindow() { InitializeComponent(); }

        public VMs.CardsClaimWindow VM => (VMs.CardsClaimWindow) DataContext;
        private void StartBattle(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
