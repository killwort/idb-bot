using IBDTools.Screens;

namespace IBDTools.Workers {
    public class OpponentWithHistory {
        public Opponent Opponent { get; }
        public double AvgChange { get; }
        public OpponentWithHistory(Opponent opponent, double avgChange) {
            Opponent = opponent;
            AvgChange = avgChange;
        }
    }
}
