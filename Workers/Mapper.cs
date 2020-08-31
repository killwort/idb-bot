using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using IBDTools.VMs;
using log4net;
using Newtonsoft.Json;

namespace IBDTools.Workers {
    public class Mapper : IWorker {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mapper));

        private static readonly Rectangle SubScanBounds = new Rectangle(-2, -1, 4, 5);
        public int Period = 20;
        public int Phase = 0;
        public int ProcessedTiles = 0;
        public int RecognizedTiles = 0;
        public int FailedTiles = 0;
        public int TotalTiles = 0;

        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            using (NDC.Push("Mapper Worker")) {
                await Task.Delay(1);
                var map = new WorldMap(context);
                var stride = (int) Math.Ceiling(484D / SubScanBounds.Width);
                var scans = (int) Math.Ceiling(484D / SubScanBounds.Height);
                var linear = Phase;
                ProcessedTiles = RecognizedTiles = FailedTiles = 0;
                TotalTiles = 484 * 484 / Period;

                TileInfo[][] mapData = null;
                var clog = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "map.json");
                var ilog = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "tiles");
                var jss = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
                if (!Directory.Exists(Path.GetDirectoryName(clog)))
                    Directory.CreateDirectory(clog);
                if (!Directory.Exists(ilog))
                    Directory.CreateDirectory(ilog);
                if (File.Exists(clog))
                    try {
                        mapData = JsonConvert.DeserializeObject<TileInfo[][]>(File.ReadAllText(clog), jss);
                    } catch {
                    }

                if (mapData == null || mapData.Length != 492)
                    mapData = new TileInfo[492][];
                for (var i = 0; i < mapData.Length; i++)
                    if (mapData[i] == null || mapData[i].Length != 492)
                        mapData[i] = new TileInfo[492];

                while (!cancellationToken.IsCancellationRequested) {
                    if (linear > stride * scans) break;
                    var cx = 8 + (linear % stride) * SubScanBounds.Width - SubScanBounds.X;
                    var cy = 8 + (linear / stride) * SubScanBounds.Height - SubScanBounds.Y;
                    statusUpdater($"Mapping at {cx},{cy} ({linear})");

                    if (!map.IsScreenActive()) {
                        await Task.Delay(500, cancellationToken);
                        if (!map.IsScreenActive()) {
                            Logger.Fatal("World map screen not detected! Bailing out.");
                            throw new InvalidOperationException("Not on the world map screen");
                        }
                    }

                    var needTiles = 0;
                    for (var xx = SubScanBounds.Left; xx < SubScanBounds.Right; xx++)
                    for (var yy = SubScanBounds.Top; yy < SubScanBounds.Bottom; yy++) {
                        if (xx + cx < 8 || xx + cx > 492 || yy + cy < 8 || yy + cy > 492) continue;
                        ProcessedTiles++;
                        if (mapData[cx + xx][cy + yy] == null) {
                            needTiles++;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (needTiles > 0) {
                        statusUpdater($"Mapping {needTiles} tiles at {cx},{cy} ({linear})");
                        await map.GoTo(cx, cy, cancellationToken);

                        for (var xx = SubScanBounds.Left; xx < SubScanBounds.Right; xx++)
                        for (var yy = SubScanBounds.Top; yy < SubScanBounds.Bottom; yy++) {
                            if (xx + cx < 8 || xx + cx > 492 || yy + cy < 8 || yy + cy > 492) continue;
                            mapData[cx + xx][cy + yy] = await map.SelectTile(xx, yy, Path.Combine(ilog, $"tile{cx + xx},{cy + yy}"), cancellationToken);
                            if (mapData[cx + xx][cy + yy] != null) {
                                mapData[cx + xx][cy + yy].CenterPoint = new Point(cx + xx, cy + yy);
                                RecognizedTiles++;
                            } else {
                                FailedTiles++;
                            }
                        }

                        File.WriteAllText(clog, JsonConvert.SerializeObject(mapData, jss));
                    }

                    linear += Period;
                }
            }
        }
    }
}
