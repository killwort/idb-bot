using System;
using System.Collections.Generic;
using System.Linq;
using IBDTools.Screens;

namespace IBDTools.Workers {
    public class PowerArenaStrategy : IArenaStrategy{
        public Tuple<Opponent, bool> SelectOpponent(IEnumerable<OpponentWithHistory> opponentsWithHistory, long myPower, long myScore) {
            var minPowerOpponent = opponentsWithHistory.Where(x => x.Opponent.Power > 0 && x.AvgChange >= 0).OrderBy(x => x.Opponent.Power).FirstOrDefault();
            if (minPowerOpponent != null && minPowerOpponent.Opponent.Power < 0.9 * myPower) {
                return Tuple.Create(minPowerOpponent.Opponent, true);
            }

            return Tuple.Create((Opponent) null, true);
        }
    }
}