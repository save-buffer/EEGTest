using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Filtering;

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

        static OnlineFilter Notch = OnlineFilter.CreateBandstop(ImpulseResponse.Infinite, 125, 59, 61);
        static OnlineFilter BandPass = OnlineFilter.CreateBandpass(ImpulseResponse.Infinite, 125, 0.5, 30);

        public static void Assert(bool x)
        {
            if (x)
                return;
            throw new Exception("Assertion was false!");
        }

        public static double[] GetChannel(int channel, double[,] data)
        {
            double[] result = new double[data.GetUpperBound(1) + 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = data[channel, i];
            return result;
        }

        public static void SetChannel(int channel, double[,] data, double[] channel_data)
        {
            for (int i = 0; i < channel_data.Length; i++)
                data[channel, i] = channel_data[i];
        }

        //Data is in the format [Channel Number, Sample Number]
        public static double[,] ReadDataFromFile(string File)
        {
            string d = System.IO.File.ReadAllText(File);
            double[] arr = (from s in d.Split(null) where s != "" select double.Parse(s) * 10.0e1).ToArray();
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

        // Hz = Frequency, Fs = Sampling Rate, N = Size of FFT, n = index
        // Hz = n * Fs / N => n = N * Hz / Fs
        static int FFTIndex(double Hz, int SamplingRate, int N) => (int)Math.Round(N * Hz / SamplingRate);

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
                x = Notch.ProcessSamples(x);
                x = BandPass.ProcessSamples(x);
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

        const int chunk_size = 38;
        //Splits the samples up into chunks of 38 for about 300 ms
        //Study has 80% overlap of time windows, so 7-8 are thrown out for us
        static FFT_Channel[,] CreateFFTData(double[,] RawData)
        {
            FFT_Channel[,] Result = new FFT_Channel[16, (RawData.GetUpperBound(1) + 1) / 16];

            for (int i = 0; i < (RawData.GetUpperBound(1) + 1) / chunk_size; i++)
            {
                int first = i * chunk_size;
                var x = SampleRange(first, first + chunk_size - 1, RawData);
                var channels = SelectFFT_Sample(x);
                for (int j = 0; j < 16; j++)
                {
                    Result[j, i] = channels[j];
                }
            }
            return Result;
        }
        int BrainWaves = 4;
        // data = [16 x N], result = [N x (W * 16)]
        static double[,] PackFFTDataForNeuralNet(FFT_Channel[,] data)
        {
            double[,] result = new double[data.GetUpperBound(1) + 1, (data.GetUpperBound(0) + 1) * 4];
            for (int i = 0; i < data.GetUpperBound(1) + 1; i++)
            {
                for (int j = 0; j < data.GetUpperBound(0) + 1; j++)
                {
                    //result[i, 6 * j] = data[j, i].Delta;
                    //result[i, 6 * j + 1] = data[j, i].Theta;
                    result[i, 4 * j] = data[j, i].Alpha;
                    result[i, 4 * j + 1] = data[j, i].SMR_Beta;
                    result[i, 4 * j + 2] = data[j, i].MID_Beta;
                    result[i, 4 * j + 3] = data[j, i].HI_Beta;
                }
            }
            return result;
        }

        static double[] FatSamplesToPackedNNData(FatSample[] samples)
        {
            double[,] raw = new double[16, samples.Length];
            for (int i = 0; i < raw.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < 16; j++)
                    raw[i, j] = samples[i].Channels[j];
            }
            var _res = RawToPackedNNData(raw);
            double[] result = new double[96];
            for (int i = 0; i < 96; i++)
                result[i] = _res[0, i];
            return result;
        }

        static double[,] RawToPackedNNData(double[,] raw) => PackFFTDataForNeuralNet(CreateFFTData(raw));

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
            const int samples_to_collect = 20000;
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
            //CollectData("Up.txt");
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
                    //new int[] { 64  , 100, 2 },
                    //new int[] { 64  , 40, 2 },
                    //new int[] { 64  , 48, 24, 12, 6, 2 },
                    //new int[] { 64  , 100, 120, 130, 100, 2 }
                    new int[] { 64, 33, 2 }
                }, 2000, new double[] { 1.0 });
        }

        static void EEGTestWithNN2()
        {
            Console.WriteLine("Training Neural Network");
            var n = NNCollectedData2();
            Console.WriteLine($"Percent correct: {n.PercentCorrect()}");
            Console.WriteLine("Awaiting Connection");
            EEG eeg = new EEG(COM);
            eeg.Start();
            Console.WriteLine("Connected");
            Stopwatch s = new Stopwatch();
            s.Start();
            while (s.ElapsedMilliseconds < 60000)
            {
                FatSample[] f = new FatSample[chunk_size];
                for (int i = 0; i < f.Length; i++)
                    f[i] = new FatSample(eeg.ReadSample(), eeg.ReadSample());
                Console.WriteLine(n.Predict(FatSamplesToPackedNNData(f)) == 0 ? "Up" : "Down");
            }
        }

        static void Main(string[] args)
        {
            //            var n = NNCollectedData2();
            //          Console.WriteLine($"Percent correct: {n.PercentCorrect()}");
            //CollectData2();
            EEGTestWithNN2();
            Console.ReadLine();
        }
    }
}
