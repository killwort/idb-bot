using System;
using System.Drawing;

namespace IBDTools.Screens {
    public abstract class VerifyByTextScreen : SnapshotOnActivationScreen {
        protected VerifyByTextScreen(GameContext context) : base(context) { }

        protected abstract Rectangle RequiredTextBox { get; }
        protected abstract string RequiredText { get; }

        public override bool IsScreenActive(Bitmap screen) {
            var text = Context.TextFromBitmap(screen, RequiredTextBox);
            return string.Equals(text, RequiredText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}