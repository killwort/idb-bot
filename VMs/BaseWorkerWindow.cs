using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IBDTools.Workers;
using log4net;

namespace IBDTools.VMs {
    public abstract class BaseWorkerWindow : DependencyObject {
        public static readonly DependencyProperty MainMessageProperty = DependencyProperty.Register("MainMessage", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsNotRunningProperty = DependencyProperty.Register("IsNotRunning", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(true));
        private CancellationTokenSource _cancel;
        private GameContext GameContext => MainWindow.GameContext;
        public string MainMessage { get => (string) GetValue(MainMessageProperty); set => SetValue(MainMessageProperty, value); }
        public string Status { get => (string) GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
        public bool IsNotRunning { get => (bool) GetValue(IsNotRunningProperty); set => SetValue(IsNotRunningProperty, value); }

        protected abstract IWorker CreateWorker();

        public async Task Start() {
            _cancel?.Cancel();
            _cancel = new CancellationTokenSource();
            var worker = CreateWorker();
            Dispatcher.Invoke(() => IsNotRunning = false);
            try {
                await worker.Run(GameContext, this, s => Dispatcher.Invoke(() => Status = s), _cancel.Token);
                Dispatcher.Invoke(() => MainMessage = "Finished!");
            } catch (TaskCanceledException e) {
                Dispatcher.Invoke(() => MainMessage = "Cancelled!");
            } catch (Exception e) {
                Dispatcher.Invoke(() => MainMessage = e.GetType().Name + ": " + e.Message);
            } finally {
                Dispatcher.Invoke(() => IsNotRunning = true);
            }
        }

        public void Stop() {
            LogManager.GetLogger(GetType()).Warn("Stop event received!");
            _cancel?.Cancel();
        }
    }
}