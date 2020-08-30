using System.Runtime.InteropServices;

namespace IBDTools {
    namespace VMs {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X { get; set; }
        public int Y { get; set; }
    }
}