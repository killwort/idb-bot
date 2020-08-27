using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IBDTools.Workers;
using log4net;

namespace IBDTools.VMs {
    public class ArenaWindow :DependencyObject{
        private GameContext GameContext => MainWindow.GameContext;
        public string MainMessage { get { return (string) GetValue(MainMessageProperty); } set { SetValue(MainMessageProperty, value); } }
        public long MaxScore { get { return (long) GetValue(MaxScoreProperty); } set { SetValue(MaxScoreProperty, value); } }
        public long MinTickets { get { return (long) GetValue(MinTicketsProperty); } set { SetValue(MinTicketsProperty, value); } }
        public string Status { get { return (string) GetValue(StatusProperty); } set { SetValue(StatusProperty, value); } }
        public bool IsNotRunning { get { return (bool) GetValue(IsNotRunningProperty); } set { SetValue(IsNotRunningProperty, value); } }

        private CancellationTokenSource _cancel;
        private Arena _worker;
        public static readonly DependencyProperty MainMessageProperty = DependencyProperty.Register("MainMessage", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty MaxScoreProperty = DependencyProperty.Register("MaxScore", typeof(long), typeof(ArenaWindow), new PropertyMetadata(100000L));
        public static readonly DependencyProperty MinTicketsProperty = DependencyProperty.Register("MinTickets", typeof(long), typeof(ArenaWindow), new PropertyMetadata(0L));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsNotRunningProperty = DependencyProperty.Register("IsNotRunning", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(true));

        public async Task Start() {
            _cancel?.Cancel();
            _cancel = new CancellationTokenSource();
            _worker = new Arena {
                MaxScore = MaxScore,
                MinTickets = MinTickets
            };
            IsNotRunning = false;
            try {
                await _worker.Run(GameContext, s => Status = s, _cancel.Token);
                MainMessage = "Goal reached, stopped";
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
