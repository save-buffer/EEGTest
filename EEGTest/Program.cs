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

        const double Delta = 3.0;
        const double Theta = 7.0;
        const double Alpha = 12.0;
        const double SMR_Beta = 15.0;
        const double MID_Beta = 18.0;
        const double HI_Beta = 35.0;
        //Gamma?

        public static void Assert(bool x)
        {
            if (x)
                return;
            throw new Exception("Assertion was false!");
        }

        //Data is in the format [Channel Number, Sample Number]
        public static double[,] ReadDataFromFile(string File)
        {
            string d = System.IO.File.ReadAllText(File);
            double[] arr = (from s in d.Split(null) where s != "" select double.Parse(s) * 10.0e2).ToArray();
            Assert(arr.Length % 16 == 0);
            double[,] data = new double[16, arr.Length / 16];
            for (int i = 0; i < arr.Length; i++)
                data[i % 16, i / 16] = arr[i];
            return data;
        }

        public static double[,] SampleRange(int first, int last, double[,] samples)
        {
            double[,] result = new double[samples.GetUpperBound(0) + 1, last - first + 1];
            for (int i = first; i <= last; i++)
            {
                for (int j = 0; j < result.GetUpperBound(0) + 1; j++)
                    result[j, i - first] = samples[j, i];
            }
            return result;
        }


        public static double[,] ConcatenateArrays(double[,] a, double[,] b)
        {
            double[,] result = new double[a.GetUpperBound(0) + b.GetUpperBound(0) + 2, a.GetUpperBound(1) + 1];
            for (int i = 0; i < a.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < a.GetUpperBound(1) + 1; j++)
                    result[i, j] = a[i, j];
            }
            for (int i = 0; i < b.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < b.GetUpperBound(1) + 1; j++)
                    result[i + a.GetUpperBound(0) + 1, j] = b[i, j];
            }
            return result;
        }

        //Hz = Frequency, Fs = Sampling Rate, N = Size of FFT, n = index
        // Hz = n * Fs / N => n = N * Hz / Fs
        static int FFTIndex(double Hz, int SamplingRate, int N) => (int)(N * Hz / SamplingRate);

        public static double[] FFT(double[] data)
        {
            Complex[] samples = (from x in data select new Complex(x, 0)).ToArray();
            Fourier.Forward(samples);
            return (from x in samples select x.Magnitude).ToArray();
        }

        public static double[][] FFTData(double[,] data)
        {
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

        static FFT_Channel[] SelectFFT_Sample(double[,] RawData)
        {
            var fft = FFTData(RawData);
            FFT_Channel[] result = new FFT_Channel[fft.Length];
            for (int i = 0; i < fft.Length; i++)
            {
                result[i].Delta = fft[i][FFTIndex(Delta, 125, fft[i].Length)];
                result[i].Theta = fft[i][FFTIndex(Theta, 125, fft[i].Length)];
                result[i].Alpha = fft[i][FFTIndex(Alpha, 125, fft[i].Length)];
                result[i].SMR_Beta = fft[i][FFTIndex(SMR_Beta, 125, fft[i].Length)];
                result[i].MID_Beta = fft[i][FFTIndex(MID_Beta, 125, fft[i].Length)];
                result[i].HI_Beta = fft[i][FFTIndex(HI_Beta, 125, fft[i].Length)];
            }
            return result;
        }

        //Splits the samples up into chunks of 16
        static FFT_Channel[,] CreateFFTData(double[,] RawData)
        {
            FFT_Channel[,] Result = new FFT_Channel[16, (RawData.GetUpperBound(1) + 1) / 16];
            for (int i = 0; i < (RawData.GetUpperBound(1) + 1) / 16; i++)
            {
                int first = i * 16;
                var x = SampleRange(first, first + 15, RawData);
                var channels = SelectFFT_Sample(x);
                for (int j = 0; j < 16; j++)
                {
                    Result[j, i] = channels[j];
                }
            }
            return Result;
        }

        // data = [16 x N], result = [N x 96]
        static double[,] PackFFTDataForNeuralNet(FFT_Channel[,] data)
        {
            double[,] result = new double[data.GetUpperBound(1) + 1, (data.GetUpperBound(0) + 1) * 6];
            for (int i = 0; i < data.GetUpperBound(1) + 1; i++)
            {
                for (int j = 0; j < data.GetUpperBound(0) + 1; j += 6)
                {
                    result[i, j] = data[j, i].Delta;
                    result[i, j + 1] = data[j, i].Theta;
                    result[i, j + 2] = data[j, i].Alpha;
                    result[i, j + 3] = data[j, i].SMR_Beta;
                    result[i, j + 4] = data[j, i].MID_Beta;
                    result[i, j + 5] = data[j, i].HI_Beta;
                }
            }
            return result;
        }

        static double[,] RawToPackedNNData(double[,] raw)
        {
            return PackFFTDataForNeuralNet(CreateFFTData(raw));
        }

        static void TestFFT()
        {
            var data = ReadDataFromFile("Up.txt");
            var fft = FFTData(data);
            for (int i = 0; i < fft.Length; i++)
            {
                Console.WriteLine($"Channel {i}:");
                Console.WriteLine($"\tDelta   : {fft[i][FFTIndex(Delta, 125, fft[i].Length)]}");
                Console.WriteLine($"\tTheta   : {fft[i][FFTIndex(Theta, 125, fft[i].Length)]}");
                Console.WriteLine($"\tAlpha   : {fft[i][FFTIndex(Alpha, 125, fft[i].Length)]}");
                Console.WriteLine($"\tSMR_Beta: {fft[i][FFTIndex(SMR_Beta, 125, fft[i].Length)]}");
                Console.WriteLine($"\tMID_Beta: {fft[i][FFTIndex(MID_Beta, 125, fft[i].Length)]}");
                Console.WriteLine($"\tHI_Beta : {fft[i][FFTIndex(HI_Beta, 125, fft[i].Length)]}");
                Console.WriteLine();
            }

        }

        public static void CollectData(string File)
        {
            const int samples_to_collect = 500;
            EEG eeg = new EEG(COM);
            eeg.Start();
            Console.WriteLine("Connected");
            List<FatSample> all_samples = new List<FatSample>();
            for (int j = 0; j < samples_to_collect; j++)
            {
                FatSample f = new FatSample(eeg.ReadSample(), eeg.ReadSample());
                Console.WriteLine(j);
                //Console.WriteLine("INDEX: " + index);
                all_samples.Add(f);
                //Console.WriteLine(f);
                //Console.WriteLine();
            }
            var filestream = System.IO.File.AppendText(File);
            foreach (var i in all_samples)
                filestream.Write(i.ToSpaceSeparated());
            filestream.Flush();
            filestream.Close();
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
            var raw_down = ReadDataFromFile("Down.txt");
            var fft_up = RawToPackedNNData(raw_up);
            var fft_down = RawToPackedNNData(raw_down);
            var training_set = ConcatenateArrays(fft_up, fft_down);
            var Y = new int[training_set.GetUpperBound(0) + 1];
            for (int i = 0; i < Y.Length; i++)
            {
                Y[i] = (i < fft_up.GetUpperBound(0) + 1) ? 0 : 1;
            }
            return NeuralNet.ChooseBestNeuralNet(training_set, Y, new int[][]
                {
                    new int[] { 96, 100, 2 },
                    new int[] { 96, 40, 2},
                    new int[] { 96, 48, 24, 12, 6, 2}
                }, 200);
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

        static void Main(string[] args)
        {
            var n = NNCollectedData2();
            Console.WriteLine($"Percent correct: {n.PercentCorrect()}");
            //CollectData2(); 
            Console.ReadLine();
        }
    }
}
