using System;
using System.Runtime.InteropServices;

namespace Lucene.Net.Support
{
    public static class FloatUtils
    {
        [DllImport("msvcrt")]
        public static extern IntPtr _controlfp_s(IntPtr currentControl, int newControl, int mask);

        public static void SetPrecision()
        {
            //if (!IsLinux())
            {
                // precision control
                const int _MCW_PC = 0x00030000;
                const int _PC_24 = 0x00020000;

                _controlfp_s(IntPtr.Zero, _PC_24, _MCW_PC);
            }
            //else
            {
                // LUCENENET TODO: implement setting float precision
                // on *nix systems.
            }
        }
    }
}
