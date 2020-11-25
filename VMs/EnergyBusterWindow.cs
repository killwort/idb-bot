using IBDTools.Workers;

namespace IBDTools.VMs {
    public class EnergyBusterWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new EnergyBuster();
    }
}
