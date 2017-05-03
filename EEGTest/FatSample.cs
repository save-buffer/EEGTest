using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EEGTest
{
    class FatSample
    {
        public double[] Channels = new double[16];
        public double AX, AY, AZ;

        public FatSample()
        {

        }

        public string ToCSV()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Channels.Length; i++)
                sb.Append(Channels[i] + ",");
            sb.Remove(sb.Length - 1, 1);
            sb.Append('\n');
            return sb.ToString();
        }

        public string ToSpaceSeparated()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Channels.Length; i++)
            {
                sb.Append(Channels[i]);
                sb.Append(' ');
            }
            sb.Append('\n');
            return sb.ToString();
        }

        public FatSample(Sample a, Sample b)
        {
            for (int i = 0; i < 8; i++)
            {
                Channels[i] = a.Channels[i];
            }
            for (int i = 8; i < 16; i++)
            {
                Channels[i] = b.Channels[i % 8];
            }
            AX = (a.AX + b.AX) / 2;
            AY = (a.AY + b.AY) / 2;
            AZ = (a.AZ + b.AZ) / 2;
        }

        public static FatSample Average(FatSample[] samples)
        {
            FatSample result = new FatSample();
            for (int i = 0; i < 16; i++)
            {
                for(int j = 0; j < samples.Length; j++)
                {
                    result.Channels[i] += samples[j].Channels[i];
                }
                result.Channels[i] /= samples.Length;
            }
            for (int j = 0; j < samples.Length; j++)
            {
                result.AX += samples[j].AX;
                result.AY += samples[j].AY;
                result.AZ += samples[j].AZ;
            }
            result.AX /= samples.Length;
            result.AY /= samples.Length;
            result.AZ /= samples.Length;
            return result;
        }

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < 16; i++)
            {
                result += "Channel " + i + ": " + Channels[i] + "\n";
            }
            result += "Accelerometer: (" + AX + ", " + AY + ", " + AZ + ")\n";
            return result;
        }
    }
}
