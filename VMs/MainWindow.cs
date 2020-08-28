using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IBDTools.VMs {
    public class MainWindow:DependencyObject {
        public static GameContext GameContext { get; private set; }
        public string ConnectionStatus { get { return (string) GetValue(ConnectionStatusProperty); } set { SetValue(ConnectionStatusProperty, value); } }
        public bool IsConnectedToGame { get { return (bool) GetValue(IsConnectedToGameProperty); } set { SetValue(IsConnectedToGameProperty, value); } }

        public MainWindow() {
            GameContext = new GameContext();
        }

        private Task _connectionTask;
        private CancellationTokenSource _cancelConnection;
        public static readonly DependencyProperty ConnectionStatusProperty = DependencyProperty.Register("ConnectionStatus", typeof(string), typeof(MainWindow), new PropertyMetadata("Not connected"));
        public static readonly DependencyProperty IsConnectedToGameProperty = DependencyProperty.Register("IsConnectedToGame", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public void StartConnecting() {
            if (!IsConnectedToGame && _connectionTask==null) {
                _cancelConnection?.Cancel();
                _cancelConnection= new CancellationTokenSource();
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
                    },_cancelConnection.Token
                );
            }
        }

        public void StopConnecting() {
            _cancelConnection.Cancel();
            _connectionTask = null;
        }

    }
}
