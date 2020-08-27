using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IBDTools.Workers;
using log4net;

namespace IBDTools.VMs {
    public class TreasureLootWindow :DependencyObject{
        private GameContext GameContext => MainWindow.GameContext;
        public string MainMessage { get { return (string) GetValue(MainMessageProperty); } set { SetValue(MainMessageProperty, value); } }
        public string Status { get { return (string) GetValue(StatusProperty); } set { SetValue(StatusProperty, value); } }
        public bool IsNotRunning { get { return (bool) GetValue(IsNotRunningProperty); } set { SetValue(IsNotRunningProperty, value); } }

        private CancellationTokenSource _cancel;
        private TreasureMapLooter _worker;
        public static readonly DependencyProperty MainMessageProperty = DependencyProperty.Register("MainMessage", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsNotRunningProperty = DependencyProperty.Register("IsNotRunning", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(true));

        public async Task Start() {
            _cancel?.Cancel();
            _cancel = new CancellationTokenSource();
            _worker = new TreasureMapLooter();
            IsNotRunning = false;
            try {
                await _worker.Run(GameContext, s => Status = s, _cancel.Token);
                MainMessage = "No maps left!";
            } catch (Exception e) {
                MainMessage = e.GetType().Name + ": " + e.Message;
            } finally {
                IsNotRunning = true;
            }
        }
        public void Stop() {
            LogManager.GetLogger(GetType()).Warn("Stop event received!");
            _cancel?.Cancel();
        }
    }
}
