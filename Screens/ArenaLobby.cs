using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class ArenaLobby : VerifyByTextScreen {
        private static readonly Rectangle ScoreBox = new Rectangle(720, 333, 120, 33);
        private static readonly Point BattleButton = new Point(737, 394);
        public ArenaLobby(GameContext context) : base(context) { }
        protected override Rectangle RequiredTextBox => new Rectangle(119, 78, 98, 36);
        protected override string RequiredText => "arena";

        public long CurrentScore => Context.NumberFromBitmap(CurrentScreen, ScoreBox);
        public Task PressBattleButton(CancellationToken cancellationToken) => Context.ClickAt(BattleButton, cancellationToken);
    }
}