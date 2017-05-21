using System.Runtime.InteropServices;

namespace EEGTest
{
    [StructLayout(LayoutKind.Explicit)]
    struct FFT_Channel
    {
        [FieldOffset(0)]
        public double Delta;
        [FieldOffset(8)]
        public double Theta;
        [FieldOffset(16)]
        public double Alpha;
        [FieldOffset(24)]
        public double SMR_Beta;
        [FieldOffset(32)]
        public double MID_Beta;
        [FieldOffset(40)]
        public double HI_Beta;
    }
}