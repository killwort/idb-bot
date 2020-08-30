using IBDTools.Workers;

namespace IBDTools.VMs {
    public class TreasureLootWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new TreasureMapLooter();
    }
}
