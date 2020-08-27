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
            /*var sz = RequiredTextBox.Size;
            using (var c = new Bitmap(sz.Width, sz.Height)) {
                using (var gr = Graphics.FromImage(c)) {
                    gr.DrawImage(screen, new Rectangle(new Point(0, 0), sz), RequiredTextBox, GraphicsUnit.Pixel);

                }
                c.Save("d:\\gr.png",ImageFormat.Png);
            }*/

            string text = Context.TextFromBitmap(screen, RequiredTextBox);
            return string.Equals(text, RequiredText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
