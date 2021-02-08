using System.Windows;
using IBDTools.Workers;
using Newtonsoft.Json.Linq;

namespace IBDTools.VMs {
    public class EnergyBusterWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new EnergyBuster {

        };
        public static readonly DependencyProperty DismissExchangesProperty = DependencyProperty.Register("DismissExchanges", typeof(bool), typeof(EnergyBusterWindow), new PropertyMetadata(default(bool)));

        public bool DismissExchanges { get { return (bool) GetValue(DismissExchangesProperty); } set { SetValue(DismissExchangesProperty, value); } }

        public static readonly DependencyProperty DismissBartersProperty = DependencyProperty.Register("DismissBarters", typeof(bool), typeof(EnergyBusterWindow), new PropertyMetadata(default(bool)));

        public bool DismissBarters { get { return (bool) GetValue(DismissBartersProperty); } set { SetValue(DismissBartersProperty, value); } }

        protected override void LoadSettings(JObject o) {
            DismissBarters = o["DismissBarters"]?.Value<bool>() ?? DismissBarters;
            DismissExchanges = o["DismissExchanges"]?.Value<bool>() ?? DismissExchanges;
        }

        protected override JObject SaveSettings() =>
            JObject.FromObject(
                new {
                    DismissBarters,
                    DismissExchanges
                }
            );
    }
}
