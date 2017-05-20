using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using MathNet.Numerics.IntegralTransforms;

namespace EEGTest
{
    class Program
    {
        const string COM = "COM11";

        public static void Assert(bool x)
        {
            if (x)
                return;
            throw new Exception("Assertion was false!");
        }

        public static double[] FFT(double[] data)
        {
            Complex[] samples = (from x in data select new Complex(x, 0)).ToArray();
            Fourier.Forward(samples);
            return (from x in samples select x.Magnitude).ToArray();
        }

        public static double[][] FFTData(double[,] data)
        {
            /*
            1 2 3 4 5 6      1 1 1
            1 2 3 4 5 6  =>  2 2 2 ...
            1 2 3 4 5 6      3 3 3
            */
            double[][] result = new double[data.GetUpperBound(0) + 1][];
            double[] x = new double[data.GetUpperBound(1) + 1];
            for (int i = 0; i < data.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < x.Length; j++)
                    x[j] = data[i, j];
                result[i] = FFT(x);
            }
            return result;
        }

        public static void CollectData(string File)
        {
            const int samples_to_collect = 1000;
            EEG eeg = new EEG(COM);
            eeg.Start();
            Console.WriteLine("Connected");
            int index = 0;
            List<FatSample> all_samples = new List<FatSample>();
            for (int j = 0; j < samples_to_collect; j++)
            {
                FatSample f = new FatSample(eeg.ReadSample(), eeg.ReadSample());
                Console.WriteLine("INDEX: " + index);
                all_samples.Add(f);
                Console.WriteLine(f);
                Console.WriteLine();
            }
            var filestream = System.IO.File.AppendText(File);
            foreach (var i in all_samples)
                filestream.Write(i.ToSpaceSeparated());
            eeg.Stop();
        }

        static void CollectData2()
        {
            Console.WriteLine("Ready?");
            Console.ReadLine();
            CollectData("Up.txt");
            Console.WriteLine("Now we're training going down");
            Console.ReadLine();
            CollectData("Down.txt");
            Console.WriteLine("Done");
        }

        static NeuralNet NNCollectedData2()
        {
            var raw_up = ReadDataFromFile("Up.txt");
            var down = ReadDataFromFile("Down.txt");
            var up = FFTData(raw_up);
        }

        static void EEGTestWithNN2()
        {
            EEG eeg = new EEG(COM);
            eeg.Start();
            var n = NNCollectedData2();
            Stopwatch s = new Stopwatch();
            s.Start();
            while (s.ElapsedMilliseconds < 60000)
            {
                FatSample f = new FatSample(eeg.ReadSample(), eeg.ReadSample());

                Console.WriteLine(n.Predict(f.Channels) == 0 ? "Up" : "Down");
            }
        }

        public static double[,] ReadDataFromFile(string File)
        {
            string d = System.IO.File.ReadAllText(File);
            double[] arr = (from s in d.Split(null) where s != "" select double.Parse(s) * 10.0e2).ToArray();
            Assert(arr.Length % 16 == 0);
            double[,] data = new double[16, d.Length / 16];
            for (int i = 0; i < arr.Length; i++)
                data[i % 16, i / 16] = arr[i];
            return data;
        }

        const double Delta = 3.0;
        const double Theta = 7.0;
        const double Alpha = 12.0;
        const double SMR_Beta = 15.0;
        const double MID_Beta = 18.0;
        const double HI_Beta = 35.0;
        //Gamma?

        //Hz = Frequency, Fs = Sampling Rate, N = Size of FFT, n = index
        // Hz = n * Fs / N => n = N * Hz / Fs

        static int FFTIndex(double Hz, int SamplingRate, int N) => (int)(N * Hz / SamplingRate);

        static FFT_Sample[] SelectFFT_Sample(double[,] RawData)
        {
            var fft = FFTData(RawData);
            FFT_Sample[] result = new FFT_Sample[fft.Length];
            for (int i = 0; i < fft.Length; i++)
            {
                result[i].Delta = fft[i][FFTIndex(Delta, 250, fft[i].Length)];
                result[i].Theta = fft[i][FFTIndex(Theta, 250, fft[i].Length)];
                result[i].Alpha = fft[i][FFTIndex(Alpha, 250, fft[i].Length)];
                result[i].SMR_Beta = fft[i][FFTIndex(SMR_Beta, 250, fft[i].Length)];
                result[i].MID_Beta = fft[i][FFTIndex(MID_Beta, 250, fft[i].Length)];
                result[i].HI_Beta = fft[i][FFTIndex(HI_Beta, 250, fft[i].Length)];
            }
            return result;
        }

        static void TestFFT()
        {
            var data = ReadDataFromFile("Up.txt");
            var fft = FFTData(data);
            for (int i = 0; i < fft.Length; i++)
            {
                Console.WriteLine($"Channel {i}:");
                Console.WriteLine($"\tDelta   : {fft[i][FFTIndex(Delta, 250, fft[i].Length)]}");
                Console.WriteLine($"\tTheta   : {fft[i][FFTIndex(Theta, 250, fft[i].Length)]}");
                Console.WriteLine($"\tAlpha   : {fft[i][FFTIndex(Alpha, 250, fft[i].Length)]}");
                Console.WriteLine($"\tSMR_Beta: {fft[i][FFTIndex(SMR_Beta, 250, fft[i].Length)]}");
                Console.WriteLine($"\tMID_Beta: {fft[i][FFTIndex(MID_Beta, 250, fft[i].Length)]}");
                Console.WriteLine($"\tHI_Beta : {fft[i][FFTIndex(HI_Beta, 250, fft[i].Length)]}");
                Console.WriteLine();
            }

        }

        static void Main(string[] args)
        {
            CollectData2();
            Console.ReadLine();
        }
    }
}
