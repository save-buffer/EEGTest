using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEGTest
{
    class Sample
    {
        public bool Daisy = false;
        public byte SampleNumber;
        public float[] Channels = new float[8];
        public float AX, AY, AZ;

        public override string ToString()
        {
            string result = "Daisy: " + Daisy + " SampleNumber: " + SampleNumber + "\n";
            for(int i = 0; i < 8; i++)
            {
                result += "Channel " + i + ": " + Channels[i] + "\n";
            }
            result += "Accelerometer: (" + AX + ", " + AY + ", " + AZ + ")\n";
            return result;
        }
    }
}
