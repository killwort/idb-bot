using System.Drawing;

namespace IBDTools.Screens {
    public class PrepareBattle : VerifyByTextScreen {
        private static readonly Point BattleButton=new Point(708,427);
        protected override Rectangle RequiredTextBox => new Rectangle(BattleButton, new Size(90, 33));
        protected override string RequiredText => "Battle";

        public void Engage() => Context.ClickAt(BattleButton);

        public PrepareBattle(GameContext context) : base(context) {
        }
    }
}