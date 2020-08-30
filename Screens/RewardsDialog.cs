using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace IBDTools.Screens {
    public class RewardsDialog : VerifyByTextScreen {
        private static readonly Rectangle OkBox = new Rectangle(479, 503, 28, 22);

        public RewardsDialog(GameContext context) : base(context) { }

        protected override Rectangle RequiredTextBox => OkBox;
        protected override string RequiredText => "Ok";

        public Task PressOkButton(CancellationToken cancellationToken) => Context.ClickAt(OkBox.Location, cancellationToken);
    }
}