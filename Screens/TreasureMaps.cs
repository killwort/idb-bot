using System.Drawing;

namespace IBDTools.Screens {
    public class TreasureMaps : VerifyByTextScreen {
        public TreasureMaps(GameContext context) : base(context) {
        }

        private static readonly Point LootButton=new Point(621, 255);
        private static readonly Rectangle LootBox = new Rectangle(248,232, 64, 16);
        protected override Rectangle RequiredTextBox => LootBox;
        protected override string RequiredText => "treasure";

        public void PressLootButton() => Context.ClickAt(LootButton);
    }
}
