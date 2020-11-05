using System.Windows;
using IBDTools.Workers;
using Newtonsoft.Json.Linq;

namespace IBDTools.VMs {
    public class ArenaWindow : BaseWorkerWindow {
        public static readonly DependencyProperty MaxScoreProperty = DependencyProperty.Register("MaxScore", typeof(long), typeof(ArenaWindow), new PropertyMetadata(100000L));
        public static readonly DependencyProperty MinTicketsProperty = DependencyProperty.Register("MinTickets", typeof(long), typeof(ArenaWindow), new PropertyMetadata(0L));
        public static readonly DependencyProperty UseHistoryProperty = DependencyProperty.Register("UseHistory", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty UseScoreProperty = DependencyProperty.Register("UseScore", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty LimitTicketsProperty = DependencyProperty.Register("LimitTickets", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty UseWorstEnemyProperty = DependencyProperty.Register("UseWorstEnemy", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty WorstEnemyProperty = DependencyProperty.Register("WorstEnemy", typeof(string), typeof(ArenaWindow), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty UseWorstEnemyOnlyProperty = DependencyProperty.Register("UseWorstEnemyOnly", typeof(bool), typeof(ArenaWindow), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty WorstEnemyMaxDistanceProperty = DependencyProperty.Register("WorstEnemyMaxDistance", typeof(int), typeof(ArenaWindow), new PropertyMetadata(4));
        public long MaxScore { get => (long) GetValue(MaxScoreProperty); set => SetValue(MaxScoreProperty, value); }
        public long MinTickets { get => (long) GetValue(MinTicketsProperty); set => SetValue(MinTicketsProperty, value); }
        public bool UseHistory { get => (bool) GetValue(UseHistoryProperty); set => SetValue(UseHistoryProperty, value); }
        public bool UseScore { get => (bool) GetValue(UseScoreProperty); set => SetValue(UseScoreProperty, value); }
        public bool LimitTickets { get => (bool) GetValue(LimitTicketsProperty); set => SetValue(LimitTicketsProperty, value); }
        public bool UseWorstEnemy { get { return (bool) GetValue(UseWorstEnemyProperty); } set { SetValue(UseWorstEnemyProperty, value); } }
        public string WorstEnemy { get { return (string) GetValue(WorstEnemyProperty); } set { SetValue(WorstEnemyProperty, value); } }
        public bool UseWorstEnemyOnly { get { return (bool) GetValue(UseWorstEnemyOnlyProperty); } set { SetValue(UseWorstEnemyOnlyProperty, value); } }
        public int WorstEnemyMaxDistance { get { return (int) GetValue(WorstEnemyMaxDistanceProperty); } set { SetValue(WorstEnemyMaxDistanceProperty, value); } }

        protected override IWorker CreateWorker() =>
            new Arena {
                MaxScore = MaxScore,
                MinTickets = LimitTickets ? -1 : MinTickets,
                UseHistory = UseHistory,
                UseScore = UseScore,
                WorstEnemy = UseWorstEnemy ? WorstEnemy : null,
                WorstEnemyOnly = UseWorstEnemyOnly,
                WorstEnemyMaxDist = WorstEnemyMaxDistance
            };

        protected override void LoadSettings(JObject o) {
            MaxScore = o["MaxScore"]?.Value<int>() ?? MaxScore;
            MinTickets = o["MinTickets"]?.Value<int>() ?? MinTickets;
            WorstEnemyMaxDistance = o["WorstEnemyMaxDistance"]?.Value<int>() ?? WorstEnemyMaxDistance;
            UseWorstEnemy = o["UseWorstEnemy"]?.Value<bool>() ?? UseWorstEnemy;
            UseHistory = o["UseHistory"]?.Value<bool>() ?? UseHistory;
            UseScore = o["UseScore"]?.Value<bool>() ?? UseScore;
            UseWorstEnemyOnly = o["UseHistory"]?.Value<bool>() ?? UseWorstEnemyOnly;
            WorstEnemy = o["WorstEnemy"]?.Value<string>() ?? WorstEnemy;
        }

        protected override JObject SaveSettings() => JObject.FromObject(new {
            MaxScore,
            MinTickets,
            WorstEnemy,
            WorstEnemyMaxDistance,
            UseHistory,
            UseScore,
            UseWorstEnemyOnly,
            UseWorstEnemy
        });
    }
}
