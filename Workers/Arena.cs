using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using log4net;
using Newtonsoft.Json;

namespace IBDTools.Workers {
    public class Arena : IWorker {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Arena));
        public long MaxScore;
        public long MinTickets;

        private class CombatLogItem {
            public DateTime Date;
            public string Name;
            public long Power;
            public long ScoreChange;
        }

        public async Task Run(GameContext context, Action<string> statusUpdater, CancellationToken cancellationToken) {
            using (NDC.Push("Arena Worker")) {
                await Task.CompletedTask;
                var lobby = new ArenaLobby(context);
                var matcher = new ArenaMatcher(context);
                var prepareBattle = new PrepareBattle(context);
                var lastScore = 0L;
                var lastCombatScore = 0L;
                ArenaMatcher.Opponent lastOpponent=null;
                var combatResults = new List<CombatLogItem>();
                var clog = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs","combat.jlog");
                if (!Directory.Exists(Path.GetDirectoryName(clog)))
                    Directory.CreateDirectory(Path.GetDirectoryName(clog));
                if(File.Exists(clog))
                    try {
                        combatResults.AddRange(JsonConvert.DeserializeObject<CombatLogItem[]>(File.ReadAllText(clog)));
                    }catch{}
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
                    lobby.PressBattleButton();
                    await matcher.Activation(cancellationToken);
                    if (matcher.TicketsLeft == 0) {
                        await Task.Delay(500);
                        Logger.Info("Recapture matcher screen due to possible erratic tickets reading.");
                        matcher.IsScreenActive();
                    }

                    if (matcher.TicketsLeft <= MinTickets) {
                        Logger.InfoFormat("Tickets limit reached (current {0} < min {1}). Bailing out.", matcher.TicketsLeft, MinTickets);
                        return;
                    }

                    Logger.Info("Enabling fast battle.");
                    matcher.IsFastBattleEnabled = true;

                    statusUpdater($"My power {matcher.MyPower.Pretty()}, score {lobby.CurrentScore}. {matcher.TicketsLeft} tickets left.");
                    while (!cancellationToken.IsCancellationRequested) {
                        cancellationToken.ThrowIfCancellationRequested();
                        Logger.InfoFormat("Matcher screen found opponents {0}; {1}; {2}.", matcher.Opponents[0], matcher.Opponents[1], matcher.Opponents[2]);

                        var opponentsWithHistory = matcher.Opponents.Select(
                            x => {
                                var logs = combatLookup[x.Name].Where(l => Math.Abs(1.0 - (double) l.Power / x.Power) < .1).ToArray();
                                return new {
                                    Opponent = x,
                                    AvgChange = logs.Length > 0 ? logs.Average(l => l.ScoreChange) : 0
                                };
                            }
                        ).ToArray();
                        var knownBestOpponent = opponentsWithHistory.OrderByDescending(x => x.AvgChange).FirstOrDefault();
                        if (knownBestOpponent!=null && knownBestOpponent.AvgChange > 5) {
                            Logger.InfoFormat("Engaging known opponent {0} with average score {1}.", knownBestOpponent.Opponent, knownBestOpponent.AvgChange);
                            lastOpponent = knownBestOpponent.Opponent;
                            matcher.EngageOpponent(knownBestOpponent.Opponent);
                            break;
                        }

                        var minPowerOpponent = opponentsWithHistory.Where(x => x.Opponent.Power > 0 && x.AvgChange >= 0).OrderBy(x => x.Opponent.Power).FirstOrDefault();
                        if (minPowerOpponent != null && (minPowerOpponent.Opponent.Power < 0.9 * matcher.MyPower)) {
                            Logger.InfoFormat("Engaging opponent {0} (selected by power).", minPowerOpponent);
                            lastOpponent = minPowerOpponent.Opponent;
                            matcher.EngageOpponent(minPowerOpponent.Opponent);
                            break;
                        }

                        minPowerOpponent = opponentsWithHistory.Where(x => x.Opponent.Score > 0 && x.Opponent.Power < 2 * matcher.MyPower && x.AvgChange >= 0).OrderByDescending(x => x.Opponent.Score)
                                                               .FirstOrDefault();
                        if (minPowerOpponent != null) {
                            Logger.InfoFormat("Engaging opponent {0} (selected by min score).", minPowerOpponent);
                            lastOpponent = minPowerOpponent.Opponent;
                            matcher.EngageOpponent(minPowerOpponent.Opponent);
                            break;
                        }

                        Logger.Info("Refreshing matcher as no valid opponents found.");
                        cancellationToken.ThrowIfCancellationRequested();
                        matcher.GetNewOpponents();
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
                    prepareBattle.Engage();
                    cancellationToken.ThrowIfCancellationRequested();
                    await matcher.Activation(cancellationToken);
                    Logger.Info("Trying to close matcher.");
                    while (true) {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (matcher.IsScreenActive()) {
                            matcher.Close();
                            if (lobby.IsScreenActive()) {
                                Logger.Info("Ok, we're back to lobby.");
                                break;
                            }

                            await Task.Delay(500);
                        } else {
                            Logger.Fatal("Matcher screen not detected while trying to close it! Bailing out.");
                            throw new InvalidOperationException("Not on the arena matcher screen");
                        }
                    }

                    await Task.Delay(300, cancellationToken);
                }
            }
        }
    }
}
