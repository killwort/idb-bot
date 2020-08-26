using System.Drawing;

namespace IBDTools.Screens {
    public abstract class SnapshotOnActivationScreen : IScreen {
        private Bitmap _currentScreen;
        protected readonly GameContext Context;
        protected SnapshotOnActivationScreen(GameContext context) { Context = context; }

        public bool IsScreenActive() {
            var currentScreen = Context.FullScreenshot();
            if (IsScreenActive(currentScreen)) {
                CurrentScreen = currentScreen;
                return true;
            }

            currentScreen.Dispose();
            return false;
        }

        protected Bitmap CurrentScreen {
            get => _currentScreen;
            set {
                _currentScreen?.Dispose();
                _currentScreen = value;
            }
        }

        public abstract bool IsScreenActive(Bitmap screen);
    }
}
