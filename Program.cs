namespace IBDTools {
    internal class Program {
        /*private static long _maxScore = long.MaxValue, _minTickets;
        private static ManualResetEvent _stopEvent;
        private static GameContext _gameContext;

        public static void Main(string[] args) {
            _gameContext = new GameContext();
            _gameContext.Connect();
            Console.Write("\nWelcome to the IBD arena bot.\nPlease enter target score [default = unlimited]: ");
            while (true) {
                var l = Console.ReadLine();
                if (!string.IsNullOrEmpty(l)) {
                    if (long.TryParse(l, out _maxScore) && _maxScore > 0) break;
                    Console.Write("Invalid value, try again: ");
                } else break;
            }

            Console.Write("Now enter minimum number of arena tickets to reserve [default 0]: ");
            while (true) {
                var l = Console.ReadLine();
                if (!string.IsNullOrEmpty(l)) {
                    if (long.TryParse(l, out _minTickets) && _minTickets > 0) break;
                    Console.Write("Invalid value, try again: ");
                } else break;
            }

            Console.WriteLine($"All set, now enter main arena screen (ranking) and press enter!\nThe battle will continue until reaching {_minTickets} tickets or {_maxScore} score.");
            Console.ReadLine();

            _stopEvent = new ManualResetEvent(false);
            var arena = new Arena {
                MaxScore = _maxScore,
                MinTickets = _minTickets
            };
            try {
                arena.Run(_gameContext, CancellationToken.None).Wait();
            } catch (Exception e) {
                Console.SetCursorPosition(0, 3);
                Console.WriteLine(e.Message);
            }

            Console.SetCursorPosition(0, 5);
        }*/
    }
}