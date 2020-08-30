using System.Windows;
using IBDTools.Workers;

namespace IBDTools.VMs {
    public class ArenaWindow : BaseWorkerWindow {
        public static readonly DependencyProperty MaxScoreProperty = DependencyProperty.Register("MaxScore", typeof(long), typeof(ArenaWindow), new PropertyMetadata(100000L));
        public static readonly DependencyProperty MinTicketsProperty = DependencyProperty.Register("MinTickets", typeof(long), typeof(ArenaWindow), new PropertyMetadata(0L));
        public static readonly DependencyProperty UseHistoryProperty = DependencyProperty.Register("UseHistory", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty UseScoreProperty = DependencyProperty.Register("UseScore", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(false));
        public long MaxScore { get => (long) GetValue(MaxScoreProperty); set => SetValue(MaxScoreProperty, value); }
        public long MinTickets { get => (long) GetValue(MinTicketsProperty); set => SetValue(MinTicketsProperty, value); }
        public bool UseHistory { get => (bool) GetValue(UseHistoryProperty); set => SetValue(UseHistoryProperty, value); }
        public bool UseScore { get => (bool) GetValue(UseScoreProperty); set => SetValue(UseScoreProperty, value); }

        protected override IWorker CreateWorker() =>
            new Arena {
                MaxScore = MaxScore,
                MinTickets = MinTickets
            };
    }
}
