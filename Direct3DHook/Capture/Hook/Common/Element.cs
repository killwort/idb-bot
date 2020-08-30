using System;

namespace Capture.Hook.Common {
    [Serializable]
    public abstract class Element : IOverlayElement, IDisposable {
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual bool Hidden { get; set; }

        public virtual void Frame() { }

        public virtual object Clone() => MemberwiseClone();

        ~Element() => Dispose(false);

        /// <summary>
        ///     Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">true if disposing both unmanaged and managed</param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
            }
        }

        protected void SafeDispose(IDisposable disposableObj) {
            if (disposableObj != null)
                disposableObj.Dispose();
        }
    }
}