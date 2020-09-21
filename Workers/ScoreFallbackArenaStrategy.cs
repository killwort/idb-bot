using System;
using System.Collections.Generic;
using System.Linq;
using IBDTools.Screens;

namespace IBDTools.Workers {
    public class ScoreFallbackArenaStrategy : IArenaStrategy {
        public Tuple<Opponent, bool> SelectOpponent(IEnumerable<OpponentWithHistory> opponentsWithHistory, long myPower, long myScore) {
            var minPowerOpponent = opponentsWithHistory.Where(x => x.Opponent.Score > 0 && x.Opponent.Power < 2 * myPower && x.AvgChange >= 0)
                                                       .OrderByDescending(x => x.Opponent.Score).FirstOrDefault();
            if (minPowerOpponent != null) {
                return Tuple.Create(minPowerOpponent.Opponent, true);
            }
            return Tuple.Create((Opponent) null, true);
        }
    }
}