using System.Windows;
using IBDTools.Workers;
using Newtonsoft.Json.Linq;

namespace IBDTools.VMs {
    public class WallWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new WallBattler(){ResetInterval = ResetInterval};
        public static readonly DependencyProperty ResetIntervalProperty = DependencyProperty.Register("ResetInterval", typeof(int), typeof(TreasureLootWindow), new PropertyMetadata(15));

        public int ResetInterval { get { return (int) GetValue(ResetIntervalProperty); } set { SetValue(ResetIntervalProperty, value); } }

        protected override void LoadSettings(JObject o) {
            ResetInterval = o[nameof(ResetInterval)]?.Value<int>() ?? 15;
        }

        protected override JObject SaveSettings() {
            return new JObject(new JProperty(nameof(ResetInterval), ResetInterval));
        }

    }
}
