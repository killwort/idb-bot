using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IBDTools.VMs {
    public class MainWindow : DependencyObject {
        public static readonly DependencyProperty ConnectionStatusProperty = DependencyProperty.Register("ConnectionStatus", typeof(string), typeof(MainWindow), new PropertyMetadata("Not connected"));
        public static readonly DependencyProperty IsConnectedToGameProperty = DependencyProperty.Register("IsConnectedToGame", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));
        private CancellationTokenSource _cancelConnection;

        private Task _connectionTask;

        public MainWindow() => GameContext = new GameContext();

        public static GameContext GameContext { get; private set; }
        public string ConnectionStatus { get => (string) GetValue(ConnectionStatusProperty); set => SetValue(ConnectionStatusProperty, value); }
        public bool IsConnectedToGame { get => (bool) GetValue(IsConnectedToGameProperty); set => SetValue(IsConnectedToGameProperty, value); }

        public void StartConnecting() {
            if (!IsConnectedToGame && _connectionTask == null) {
                _cancelConnection?.Cancel();
                _cancelConnection = new CancellationTokenSource();
                _connectionTask = Task.Run(
                    async () => {
                        Dispatcher.Invoke(() => ConnectionStatus = "Connecting...");
                        while (!GameContext.Connect()) {
                            _cancelConnection.Token.ThrowIfCancellationRequested();
                            await Task.Delay(500, _cancelConnection.Token);
                        }

                        Dispatcher.Invoke(
                            () => {
                                IsConnectedToGame = true;
                                ConnectionStatus = "Connected to game";
                            }
                        );
                    },
                    _cancelConnection.Token
                );
            }
        }

        public void StopConnecting() {
            _cancelConnection.Cancel();
            _connectionTask = null;
        }
    }
}