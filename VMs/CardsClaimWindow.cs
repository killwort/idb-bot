using System.Windows;
using IBDTools.Workers;
using Newtonsoft.Json.Linq;

namespace IBDTools.VMs {
    public class CardsClaimWindow : BaseWorkerWindow {
        public static readonly DependencyProperty ClaimStandardProperty = DependencyProperty.Register("ClaimStandard", typeof(bool), typeof(CardsClaimWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty ClaimHeroicProperty = DependencyProperty.Register("ClaimHeroic", typeof(bool), typeof(CardsClaimWindow), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty ClaimEventProperty = DependencyProperty.Register("ClaimEvent", typeof(bool), typeof(CardsClaimWindow), new PropertyMetadata(true));
        protected override IWorker CreateWorker() => new CardsClaimer {
            ClaimEvent = ClaimEvent,
            ClaimHeroic = ClaimHeroic,
            ClaimStandard = ClaimStandard
        };
        public bool ClaimStandard { get { return (bool) GetValue(ClaimStandardProperty); } set { SetValue(ClaimStandardProperty, value); } }
        public bool ClaimHeroic { get { return (bool) GetValue(ClaimHeroicProperty); } set { SetValue(ClaimHeroicProperty, value); } }
        public bool ClaimEvent { get { return (bool) GetValue(ClaimEventProperty); } set { SetValue(ClaimEventProperty, value); } }

        protected override void LoadSettings(JObject o) {
            ClaimStandard = o["ClaimStandard"]?.Value<bool>() ?? ClaimStandard;
            ClaimHeroic = o["ClaimHeroic"]?.Value<bool>() ?? ClaimHeroic;
            ClaimEvent = o["ClaimEvent"]?.Value<bool>() ?? ClaimEvent;
        }

        protected override JObject SaveSettings() =>
            JObject.FromObject(
                new {
                    ClaimStandard,
                    ClaimEvent,
                    ClaimHeroic
                }
            );
    }
}
