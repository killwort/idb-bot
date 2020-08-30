using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class TreasureMaps : VerifyByTextScreen {
        private static readonly Point LootButton = new Point(621, 255);
        private static readonly Rectangle LootBox = new Rectangle(248, 232, 64, 16);
        public TreasureMaps(GameContext context) : base(context) { }
        protected override Rectangle RequiredTextBox => LootBox;
        protected override string RequiredText => "treasure";

        public Task PressLootButton(CancellationToken cancellationToken) => Context.ClickAt(LootButton, cancellationToken);
    }
}