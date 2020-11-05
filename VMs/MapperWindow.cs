using System.Diagnostics;
using System.Threading;
using System.Windows;
using IBDTools.Workers;
using Newtonsoft.Json.Linq;

namespace IBDTools.VMs {
    public class MapperWindow : BaseWorkerWindow {
        public static readonly DependencyProperty PeriodProperty = DependencyProperty.Register("Period", typeof(int), typeof(MapperWindow), new PropertyMetadata(20));
        public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register("Phase", typeof(int), typeof(MapperWindow), new PropertyMetadata(default(int)));
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(int), typeof(MapperWindow), new PropertyMetadata(default(int)));
        protected override IWorker CreateWorker() => new Mapper();
        public int Period { get { return (int) GetValue(PeriodProperty); } set { SetValue(PeriodProperty, value); } }
        public int Phase { get { return (int) GetValue(PhaseProperty); } set { SetValue(PhaseProperty, value); } }
        public int Progress { get { return (int) GetValue(ProgressProperty); } set { SetValue(ProgressProperty, value); } }

        protected override void StatusUpdater() {
            var sw = new Stopwatch();
            var w = (Mapper) Worker;
            sw.Start();
            int prevValue = w.ProcessedTiles;
            while (true) {
                Thread.Sleep(1000);
                var tps = (w.ProcessedTiles - prevValue) / sw.Elapsed.TotalSeconds;
                if (sw.Elapsed.TotalSeconds > 30) {
                    sw.Restart();
                    prevValue = w.ProcessedTiles;
                }

                Dispatcher.Invoke(
                    () => {
                        MainMessage = $"Speed {tps:0.##} tiles/second, {w.ProcessedTiles} of {w.TotalTiles} processed. {w.FailedTiles} failed, {w.RecognizedTiles} OK.";
                        Progress = w.ProcessedTiles * 100 / w.TotalTiles;
                    }
                );
            }
        }

        protected override void LoadSettings(JObject o) {
            Period = o["Period"]?.Value<int>() ?? Period;
            Phase = o["Phase"]?.Value<int>() ?? Phase;
        }

        protected override JObject SaveSettings() => JObject.FromObject(new {
            Phase,
            Period
        });
    }
}
