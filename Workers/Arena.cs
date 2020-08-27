using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;

namespace IBDTools.Workers {
    public class Arena : IWorker {
        public long MaxScore;
        public long MinTickets;

        public async Task Run(GameContext context, Action<string> statusUpdater, CancellationToken cancellationToken) {
            await Task.CompletedTask;
            var lobby = new ArenaLobby(context);
            var matcher = new ArenaMatcher(context);
            var prepareBattle = new PrepareBattle(context);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!lobby.IsScreenActive())
                    throw new InvalidOperationException("Not on the arena lobby screen");
                if (lobby.CurrentScore > MaxScore) return;
                cancellationToken.ThrowIfCancellationRequested();
                lobby.PressBattleButton();
                await matcher.Activation(cancellationToken);
                if (matcher.TicketsLeft <= MinTickets)
                    return;
                statusUpdater($"My power {matcher.MyPower.Pretty()}, score {lobby.CurrentScore}. {matcher.TicketsLeft} tickets left.");
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var minPowerOpponent = matcher.Opponents.Where(x => x.Power > 0).OrderBy(x => x.Power).FirstOrDefault();
                    if (minPowerOpponent != null && !(minPowerOpponent.Power > 0.9 * matcher.MyPower)) {
                        matcher.EngageOpponent(minPowerOpponent);
                        break;
                    }

                    matcher.GetNewOpponents();
                    if (!matcher.IsScreenActive())
                        throw new InvalidOperationException("Not on the arena matcher screen");
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!prepareBattle.IsScreenActive())
                    throw new InvalidOperationException("Not on the prepare battle screen");
                cancellationToken.ThrowIfCancellationRequested();
                prepareBattle.Engage();
                cancellationToken.ThrowIfCancellationRequested();
                await matcher.Activation(cancellationToken);
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (matcher.IsScreenActive()) {
                        matcher.Close();
                        if (lobby.IsScreenActive())
                            break;
                        await Task.Delay(500);
                    } else
                        throw new InvalidOperationException("Not on the arena matcher screen");
                }

                await Task.Delay(300, cancellationToken);
            }
        }
    }
}
