using System.Drawing;

namespace IBDTools.Screens {
    public abstract class SnapshotOnActivationScreen : IScreen {
        protected readonly GameContext Context;
        private Bitmap _currentScreen;
        protected SnapshotOnActivationScreen(GameContext context) => Context = context;

        protected Bitmap CurrentScreen {
            get => _currentScreen;
            set {
                _currentScreen?.Dispose();
                _currentScreen = value;
            }
        }

        public bool IsScreenActive() {
            var currentScreen = Context.FullScreenshot();
            if (IsScreenActive(currentScreen)) {
                CurrentScreen = currentScreen;
                return true;
            }

            currentScreen.Dispose();
            return false;
        }

        public abstract bool IsScreenActive(Bitmap screen);
    }
}