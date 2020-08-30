using System;
using System.Drawing;
using Capture.Interface;

namespace Capture.Hook.Common {
    [Serializable]
    public class ImageElement : Element {
        private Bitmap _bitmap;

        private bool _ownsBitmap;

        public ImageElement() { }

        public ImageElement(string filename) : this(new Bitmap(filename), true) { Filename = filename; }

        public ImageElement(Bitmap bitmap, bool ownsImage = false) {
            Tint = Color.White;
            Bitmap = bitmap;
            _ownsBitmap = ownsImage;
            Scale = 1.0f;
        }

        /// <summary>
        ///     The image file bytes
        /// </summary>
        public virtual byte[] Image { get; set; }

        internal virtual Bitmap Bitmap {
            get {
                if (_bitmap == null && Image != null) {
                    _bitmap = Image.ToBitmap();
                    _ownsBitmap = true;
                }

                return _bitmap;
            }
            set => _bitmap = value;
        }

        /// <summary>
        ///     This value is multiplied with the source color (e.g. White will result in same color as source image)
        /// </summary>
        /// <remarks>
        ///     Defaults to <see cref="System.Drawing.Color.White" />.
        /// </remarks>
        public virtual Color Tint { get; set; } = Color.White;

        /// <summary>
        ///     The location of where to render this image element
        /// </summary>
        public virtual Point Location { get; set; }

        public float Angle { get; set; }

        public float Scale { get; set; } = 1.0f;

        public string Filename { get; set; }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                if (_ownsBitmap) {
                    SafeDispose(Bitmap);
                    Bitmap = null;
                }
            }
        }
    }
}