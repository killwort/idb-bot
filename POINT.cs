using System.Runtime.InteropServices;

namespace IBDTools {
    namespace VMs {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        private int _X;
        private int _Y;
        public int X { get => _X; set => _X = value; }
        public int Y { get => _Y; set => _Y = value; }
    }
}
