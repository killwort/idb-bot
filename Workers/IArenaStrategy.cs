using System;
using System.Collections.Generic;
using IBDTools.Screens;

namespace IBDTools.Workers {
    public interface IArenaStrategy {
        Tuple<Opponent, bool> SelectOpponent(IEnumerable<OpponentWithHistory> opponents, long myPower, long myScore);
    }
}