using System.Windows;

namespace IBDTools {
    public partial class MainWindow : Window {
        public MainWindow() => InitializeComponent();

        private VMs.MainWindow VM => (VMs.MainWindow) DataContext;

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) => VM.StartConnecting();

        private void StartArena(object sender, RoutedEventArgs e) {
            var dialog = new ArenaWindow();
            dialog.ShowDialog();
        }

        private void StartTreasureLoot(object sender, RoutedEventArgs e) {
            var dialog = new TreasureLootWindow();
            dialog.ShowDialog();
        }
    }
}