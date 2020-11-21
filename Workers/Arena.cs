using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using IBDTools.Screens;
using IBDTools.VMs;
using log4net;
using Newtonsoft.Json;

namespace IBDTools.Workers {
    public class Arena : IWorker {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Arena));
        public long MaxScore;
        public long MinTickets;
        public bool UseHistory;
        public bool UseScore;
        public string WorstEnemy;
        public bool WorstEnemyOnly;
        public int WorstEnemyMaxDist = 8;

        public async Task Run(GameContext context, BaseWorkerWindow vm, Action<string> statusUpdater, CancellationToken cancellationToken) {
            using (NDC.Push("Arena Worker")) {
                await Task.CompletedTask;
                var lobby = new ArenaLobby(context);
                var matcher = new ArenaMatcher(context);
                var prepareBattle = new PrepareBattle(context);
                var lastScore = 0L;
                var lastCombatScore = 0L;
                Opponent lastOpponent = null;
                var combatResults = new List<CombatLogItem>();
                var strategies = new List<IArenaStrategy>();
                if (!string.IsNullOrEmpty(WorstEnemy))
                    strategies.Add(new WorstEnemyArenaStrategy(WorstEnemy, WorstEnemyOnly, WorstEnemyMaxDist));
                if(UseHistory)
                    strategies.Add(new HistoryArenaStrategy());
                if (UseScore) {
                    strategies.Add(new PowerArenaStrategy());
                    strategies.Add(new ScoreFallbackArenaStrategy());
                }

                if (strategies.Any())
                    strategies.Add(new PowerArenaStrategy());
                var clog = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "combat.jlog");
                if (!Directory.Exists(Path.GetDirectoryName(clog)))
                    Directory.CreateDirectory(Path.GetDirectoryName(clog));
                if (File.Exists(clog))
                    try {
                        combatResults.AddRange(JsonConvert.DeserializeObject<CombatLogItem[]>(File.ReadAllText(clog)));
                    } catch {
                    }

                Logger.Info("Created screens, entering main loop.");
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!lobby.IsScreenActive()) {
                        Logger.Fatal("Lobby screen not detected! Bailing out.");
                        throw new InvalidOperationException("Not on the arena lobby screen");
                    }

                    if (lobby.CurrentScore > MaxScore) {
                        Logger.InfoFormat("Score limit reached (current {0} > max {1}). Bailing out.", lobby.CurrentScore, MaxScore);
                        return;
                    }

                    lastCombatScore = lobby.CurrentScore - lastScore;
                    if (lastScore != 0 && lobby.CurrentScore != 0 && lastOpponent != null) {
                        combatResults.Add(
                            new CombatLogItem {
                                Date = DateTime.UtcNow,
                                Name = lastOpponent.Name,
                                Power = lastOpponent.Power,
                                ScoreChange = lastCombatScore
                            }
                        );
                        File.WriteAllText(clog, JsonConvert.SerializeObject(combatResults, Formatting.Indented));
                    }

                    var combatLookup = combatResults.ToLookup(x => x.Name);

                    lastScore = lobby.CurrentScore;
                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.Info("Switching to matcher screen.");
                    while (true) {
                        do {
                            await lobby.PressBattleButton(cancellationToken);
                        } while (lobby.IsScreenActive());

                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        try {
                            await matcher.Activation(cts.Token);
                            break;
                        } catch (TaskCanceledException) {
                            if (lobby.IsScreenActive())
                                continue;
                            throw;
                        }
                    }

                    if (matcher.TicketsLeft == 0) {
                        await Task.Delay(500, cancellationToken);
                        Logger.Info("Recapture matcher screen due to possible erratic tickets reading.");
                        matcher.IsScreenActive();
                    }

                    if (matcher.TicketsLeft <= MinTickets) {
                        Logger.InfoFormat("Tickets limit reached (current {0} < min {1}). Bailing out.", matcher.TicketsLeft, MinTickets);
                        return;
                    }

                    Logger.Info("Enabling fast battle.");
                    await matcher.ToggleFastBattle(true, cancellationToken);

                    statusUpdater($"My power {matcher.MyPower.Pretty()}, score {lobby.CurrentScore}. {matcher.TicketsLeft} tickets left.");
                    while (!cancellationToken.IsCancellationRequested) {
                        cancellationToken.ThrowIfCancellationRequested();
                        Logger.InfoFormat("Matcher screen found opponents {0}; {1}; {2}.", matcher.Opponents[0], matcher.Opponents[1], matcher.Opponents[2]);

                        var opponentsWithHistory = matcher.Opponents.Select(
                            x => {
                                var logs = combatLookup[x.Name].Where(l => Math.Abs(1.0 - (double) l.Power / x.Power) < .1).ToArray();
                                return new OpponentWithHistory(x, logs.Length > 0 ? logs.Average(l => l.ScoreChange) : 0);
                            }
                        ).ToArray();
                        var engaged = false;
                        foreach (var strategy in strategies) {
                            var result = strategy.SelectOpponent(opponentsWithHistory, matcher.MyPower, lobby.CurrentScore);
                            if (result.Item1 != null) {
                                lastOpponent = result.Item1;
                                await matcher.EngageOpponent(result.Item1, cancellationToken);
                                engaged = true;
                                break;
                            }

                            if (!result.Item2)
                                break;
                        }

                        if (engaged) break;

                        Logger.Info("Refreshing matcher as no valid opponents found.");
                        cancellationToken.ThrowIfCancellationRequested();
                        await matcher.GetNewOpponents(cancellationToken);
                        await Task.Delay(100, cancellationToken);
                        if (!matcher.IsScreenActive()) {
                            Logger.Fatal("Matcher screen not detected after refresh! Bailing out.");
                            throw new InvalidOperationException("Not on the arena matcher screen");
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!prepareBattle.IsScreenActive()) {
                        Logger.Fatal("Prepare for battle screen not detected! Bailing out.");
                        throw new InvalidOperationException("Not on the prepare battle screen");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.Info("Into the battle!");
                    await prepareBattle.Engage(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    await matcher.Activation(cancellationToken);
                    Logger.Info("Trying to close matcher.");
                    while (true) {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (matcher.IsScreenActive()) {
                            await matcher.Close(cancellationToken);
                            if (lobby.IsScreenActive()) {
                                Logger.Info("Ok, we're back to lobby.");
                                break;
                            }

                            await Task.Delay(500, cancellationToken);
                        } else {
                            Logger.Fatal("Matcher screen not detected while trying to close it! Bailing out.");
                            throw new InvalidOperationException("Not on the arena matcher screen");
                        }
                    }

                    await Task.Delay(300, cancellationToken);
                }
            }
        }

        private class CombatLogItem {
            public DateTime Date;
            public string Name;
            public long Power;
            public long ScoreChange;
        }
    }
}
