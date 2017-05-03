using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace EEGTest
{
    class EEG
    {
        public string COMPort;
        public bool ReceivingData;
        SerialPort p;
        private int sample_index;
        public List<Sample> Samples;
        public EEG(string COMPort)
        {
            Samples = new List<Sample>();
            sample_index = 0;
            this.COMPort = COMPort;
            ReceivingData = false;
            p = new SerialPort(COMPort);
            p.BaudRate = 115200;
            p.Open();
        }

        public byte ReadByte()
        {
            return (byte)p.ReadByte();
        }

        public Sample ReadSample()
        {
            const float EEGScaleFactor = 4.5f / 24.0f / (float)((1 << 23) - 1);
            const float AccelScaleFactor = 0.002f / 16.0f;
            Sample s = new Sample();
            sample_index++;

            while (ReadByte() != 0xA0) ;
            s.Daisy = sample_index % 2 == 0;
            s.SampleNumber = ReadByte();
            for (int i = 0; i < 8; i++)
            {
                int raw_eeg = (int)(ReadByte() << 16 | ReadByte() << 8 | ReadByte());
                s.Channels[i] = EEGScaleFactor * raw_eeg;
            }
            s.AX = AccelScaleFactor * (int)(ReadByte() << 8 | ReadByte());
            s.AY = AccelScaleFactor * (int)(ReadByte() << 8 | ReadByte());
            s.AZ = (int)(ReadByte() << 8 | ReadByte());
            ReadByte();
            Samples.Add(s);
            return s;
        }

        public bool Start()
        {
            p.DiscardInBuffer();
            p.Write("v");
            for (int i = 0; i < 256;) // Read 256 bytes max. If we don't get the dollar sign by then, something is wrong.
            {
                if (ReadByte() == '$')
                {
                    if (ReadByte() == '$')
                    {
                        if (ReadByte() == '$')
                        {
                            p.Write("b");
                            ReceivingData = true;
                            return true;
                        }
                    }
                    i++;
                }
                i++;
            }
            return false;
        }

        public void Stop()
        {
            p.Write("s");
            p.Close();
            ReceivingData = false;
        }
    }
}
