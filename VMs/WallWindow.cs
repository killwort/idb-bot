using IBDTools.Workers;

namespace IBDTools.VMs {
    public class WallWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new WallBattler();
    }
}