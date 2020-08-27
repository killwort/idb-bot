using System.Drawing;

namespace IBDTools.Screens {
    public class RewardsDialog : VerifyByTextScreen {
        public RewardsDialog(GameContext context) : base(context) {
        }

        private static readonly Rectangle OkBox = new Rectangle(479, 503, 28, 22);
        protected override Rectangle RequiredTextBox => OkBox;
        protected override string RequiredText => "Ok";

        public void PressOkButton() => Context.ClickAt(OkBox.Location);
    }
}