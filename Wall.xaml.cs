using System.Windows;

namespace IBDTools {
    public partial class Wall : KbHookWindow {
        public Wall() { InitializeComponent(); }

        public VMs.WallWindow VM => (VMs.WallWindow) DataContext;
        private void StartRun(object sender, RoutedEventArgs e) => VM.Start();
        protected override void Stop() => VM.Stop();
    }
}
