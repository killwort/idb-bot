﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;
using log4net;

namespace IBDTools.Workers {
    public class Arena : IWorker {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Arena));
        public long MaxScore;
        public long MinTickets;

        public async Task Run(GameContext context, Action<string> statusUpdater, CancellationToken cancellationToken) {
            using (NDC.Push("Arena Worker")) {
                await Task.CompletedTask;
                var lobby = new ArenaLobby(context);
                var matcher = new ArenaMatcher(context);
                var prepareBattle = new PrepareBattle(context);
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
                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.Info("Switching to matcher screen.");
                    lobby.PressBattleButton();
                    await matcher.Activation(cancellationToken);
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

                        var minPowerOpponent = matcher.Opponents.Where(x => x.Power > 0).OrderBy(x => x.Power).FirstOrDefault();
                        if (minPowerOpponent != null && !(minPowerOpponent.Power > 0.9 * matcher.MyPower)) {
                            Logger.InfoFormat("Engaging opponent {0}.", minPowerOpponent);
                            matcher.EngageOpponent(minPowerOpponent);
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
