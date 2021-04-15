using System.Windows;
using IBDTools.Workers;

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

        private void StartMapper(object sender, RoutedEventArgs e) {
            var dialog = new MapperWindow();
            dialog.ShowDialog();
        }

        private void StartClaimer(object sender, RoutedEventArgs e) {
            var dialog = new CardsClaimWindow();
            dialog.ShowDialog();
        }

        private void StartEnergyBuster(object sender, RoutedEventArgs e) {
            var dialog = new EnergyBusterWindow();
            dialog.ShowDialog();
        }
        private void StartWall(object sender, RoutedEventArgs e) {
            var dialog = new Wall();
            dialog.ShowDialog();
        }
    }
}
