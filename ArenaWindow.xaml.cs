using System.Windows;

namespace IBDTools {
    public partial class ArenaWindow : KbHookWindow {
        public ArenaWindow() => InitializeComponent();

        public VMs.ArenaWindow VM => (VMs.ArenaWindow) DataContext;

        private void StartBattle(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
