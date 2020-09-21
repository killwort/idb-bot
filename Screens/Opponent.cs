namespace IBDTools.Screens {
    public class Opponent {
        internal int Index;
        public string Name;
        public long Power;
        public long Score;

        public override string ToString() => $"{Index + 1}:{Name}: Power {Power}, Score {Score}";
    }
}