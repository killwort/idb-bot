using System;
using System.Collections.Generic;
using System.Linq;
using IBDTools.Screens;

namespace IBDTools.Workers {
    public class HistoryArenaStrategy : IArenaStrategy {
        public Tuple<Opponent, bool> SelectOpponent(IEnumerable<OpponentWithHistory> opponentsWithHistory, long myPower, long myScore) {
            var knownBestOpponent = opponentsWithHistory.OrderByDescending(x => x.AvgChange).FirstOrDefault();
            if (knownBestOpponent != null && knownBestOpponent.AvgChange > 5) {
                return Tuple.Create(knownBestOpponent.Opponent, true);
            }
            return Tuple.Create((Opponent) null, true);
        }
    }

    public class WorstEnemyArenaStrategy : IArenaStrategy {
        private readonly string[] _worstEnemies;
        private readonly bool _worstEnemyOnly;
        private readonly int _maxDist;

        public WorstEnemyArenaStrategy(string worstEnemy, bool worstEnemyOnly, int maxDist) {
            _worstEnemies = worstEnemy.Split(new[] {',', ';', '|', ' '}, StringSplitOptions.RemoveEmptyEntries);
            _worstEnemyOnly = worstEnemyOnly;
            _maxDist = maxDist;
        }

        public Tuple<Opponent, bool> SelectOpponent(IEnumerable<OpponentWithHistory> opponents, long myPower, long myScore) {
            var ops = opponents.ToArray();
            foreach (var we in _worstEnemies) {
                var match = ops.FirstOrDefault(x => Extensions.DamerauLevenstein(x.Opponent.Name, we) < _maxDist);
                if(match!=null)
                    return Tuple.Create(match?.Opponent, !_worstEnemyOnly);
            }

            return Tuple.Create((Opponent) null, !_worstEnemyOnly);
        }
    }
}
