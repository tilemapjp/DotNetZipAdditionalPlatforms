namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    internal static class InternalConstants
    {
        internal const int BL_CODES = 0x13;
        internal const int D_CODES = 30;
        internal const int L_CODES = ((LITERALS + 1) + LENGTH_CODES);
        internal const int LENGTH_CODES = 0x1d;
        internal const int LITERALS = 0x100;
        internal const int MAX_BITS = 15;
        internal const int MAX_BL_BITS = 7;
        internal const int REP_3_6 = 0x10;
        internal const int REPZ_11_138 = 0x12;
        internal const int REPZ_3_10 = 0x11;
    }
}

