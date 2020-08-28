using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace IBDTools.Screens {
    public abstract class VerifyByTextScreen : SnapshotOnActivationScreen {
        protected VerifyByTextScreen(GameContext context) : base(context) {
        }

        protected abstract Rectangle RequiredTextBox { get; }
        protected abstract string RequiredText { get; }

        public override bool IsScreenActive(Bitmap screen) {
            string text = Context.TextFromBitmap(screen, RequiredTextBox);
            return string.Equals(text, RequiredText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
