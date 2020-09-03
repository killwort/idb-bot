using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IBDTools.Workers;
using log4net;

namespace IBDTools.VMs {
    public abstract class BaseWorkerWindow : DependencyObject {
        public static readonly DependencyProperty MainMessageProperty = DependencyProperty.Register("MainMessage", typeof(string), typeof(BaseWorkerWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(BaseWorkerWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsNotRunningProperty = DependencyProperty.Register("IsNotRunning", typeof(bool), typeof(BaseWorkerWindow), new PropertyMetadata(true));
        private CancellationTokenSource _cancel;
        private GameContext GameContext => MainWindow.GameContext;
        public string MainMessage { get => (string) GetValue(MainMessageProperty); set => SetValue(MainMessageProperty, value); }
        public string Status { get => (string) GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
        public bool IsNotRunning { get => (bool) GetValue(IsNotRunningProperty); set => SetValue(IsNotRunningProperty, value); }

        protected abstract IWorker CreateWorker();

        protected virtual void StatusUpdater() { }

        public async Task Start() {
            _cancel?.Cancel();
            _cancel = new CancellationTokenSource();
            Worker = CreateWorker();
            var updater = new Thread(StatusUpdater) {IsBackground = true};
            Dispatcher.Invoke(() => IsNotRunning = false);
            var logger = LogManager.GetLogger(GetType());
            try {
                updater.Start();
                await Worker.Run(GameContext, this, s => Dispatcher.Invoke(() => Status = s), _cancel.Token);
                Dispatcher.Invoke(() => MainMessage = "Finished!");
                updater.Abort();
            } catch (TaskCanceledException e) {
                updater.Abort();
                Dispatcher.Invoke(() => MainMessage = "Cancelled!");
            } catch (Exception e) {
                updater.Abort();
                logger.Fatal("Unhandled exception in the worker!", e);
                Dispatcher.Invoke(() => MainMessage = e.GetType().Name + ": " + e.Message);
            } finally {
                Dispatcher.Invoke(() => IsNotRunning = true);
            }
        }

        public IWorker Worker { get; private set; }

        public void Stop() {
            LogManager.GetLogger(GetType()).Warn("Stop event received!");
            _cancel?.Cancel();
        }
    }
}
