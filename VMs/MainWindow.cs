using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.VMs {
    public class MainWindow:INotifyPropertyChanged {
        private bool _isConnectedToGame;
        public static GameContext GameContext { get; private set; }

        public MainWindow() {
            GameContext = new GameContext();
        }

        public bool IsConnectedToGame {
            get => _isConnectedToGame;
            set {
                _isConnectedToGame = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        public bool IsConnectingToGame => _connectionTask?.IsCompleted ?? false;
        public string ConnectionStatus => IsConnectedToGame ? "Connected to game" : IsConnectingToGame ? "Connecting..." : "Not connected";

        private Task _connectionTask;
        private CancellationTokenSource _cancelConnection;

        public void StartConnecting() {
            if (!IsConnectedToGame && _connectionTask==null) {
                _cancelConnection?.Cancel();
                _cancelConnection= new CancellationTokenSource();
                _connectionTask = Task.Run(
                    async () => {
                        while (!GameContext.Connect()) {
                            _cancelConnection.Token.ThrowIfCancellationRequested();
                            await Task.Delay(500, _cancelConnection.Token);
                        }

                        IsConnectedToGame = true;
                    },_cancelConnection.Token
                );
                OnPropertyChanged(nameof(IsConnectingToGame));
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        public void StopConnecting() {
            _cancelConnection.Cancel();
            _connectionTask = null;
            OnPropertyChanged(nameof(IsConnectingToGame));
            OnPropertyChanged(nameof(ConnectionStatus));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
