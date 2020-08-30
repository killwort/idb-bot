using IBDTools.Workers;

namespace IBDTools.VMs {
    public class MapperWindow : BaseWorkerWindow {
        protected override IWorker CreateWorker() => new Mapper();
    }
}