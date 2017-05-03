using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EEGTest
{
    class Program
    {
        public enum Direction
        {
            Up, Down, Left, Right
        }
        public static Direction InterpretFatSample(FatSample s)
        {
            if (s.Channels[15] > 0.036)
                return Direction.Up;
            else
                return Direction.Left;
        }

        public static void ShowInterpreted()
        {
            EEG eeg = new EEG("COM4");
            eeg.Start();
            int index = 0;
            var filestream = System.IO.File.AppendText("Test1.txt");
            while (true)
            {
                Application.DoEvents();
                FatSample[] samples = new FatSample[64];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = new FatSample(eeg.ReadSample(), eeg.ReadSample());
                }
                Console.WriteLine("INDEX: " + index);
                FatSample f = FatSample.Average(samples);
                Direction dir = InterpretFatSample(f);
                switch (dir)
                {
                    case Direction.Up:
                        //up
                        break;
                    case Direction.Down:
                        //down
                        break;
                    case Direction.Left:
                        //left
                        break;
                    case Direction.Right:
                        //right
                        break;
                    default:
                        break;
                }
                Console.WriteLine(f);
                Console.WriteLine();
            }
        }

        public static void CollectData()
        {
            const int samples_to_collect = 1000;
            const int samples_to_average = 1;
            EEG eeg = new EEG("COM3");
            eeg.Start();
            int index = 0;
            var filestream = System.IO.File.AppendText("Down.txt");
            List<FatSample> all_samples = new List<FatSample>();
            for (int j = 0; j < samples_to_collect; j++)
            {
                FatSample[] samples = new FatSample[samples_to_average];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = new FatSample(eeg.ReadSample(), eeg.ReadSample());
                }
                Console.WriteLine("INDEX: " + index);
                FatSample f = FatSample.Average(samples);
                all_samples.Add(f);
                Console.WriteLine(f);
                Console.WriteLine();
            }
            foreach (var i in all_samples)
                filestream.Write(i.ToSpaceSeparated());
        }

        static void TestNeuralNet()
        {
            const int N = 6000;
            int[][] Layers = new int[][] { new int[] { 1, 2 }, new int[] { 1, 2, 2 }, new int[] { 1, 2, 3, 2 }, new int[] { 1, 14, 22, 2 } };
            double[,] training_set = new double[N, 1];
            int[] labels = new int[N];
            Random r = new Random();
            for (int i = 0; i < N; i++)
            {
                switch (i % 2)
                {
                    case 0:
                        training_set[i, 0] = r.NextDouble() * 50.0;
                        labels[i] = 0;
                        break;
                    case 1:
                        training_set[i, 0] = -1 * r.NextDouble() * 50.0;
                        labels[i] = 1;
                        break;
                    default:
                        break;
                }
            }
            NeuralNet n = NeuralNet.ChooseBestNeuralNet(training_set, labels, Layers, 500);
            Console.WriteLine(n.PercentCorrect() + "% Correct");
            Console.WriteLine("Accepting Input");
            for (;;)
            {

                double x = double.Parse(Console.ReadLine());
                Console.WriteLine(n.Predict(new double[] { x }));
            }
        }

        static NeuralNet NNCollectedData2()
        {
            string up = System.IO.File.ReadAllText("Up.txt");
            double[] ups = (from string s in up.Split(null) where s != "" select double.Parse(s)).ToArray();
            string down = System.IO.File.ReadAllText("Down.txt");
            double[] downs = (from string s in down.Split(null) where s != "" select double.Parse(s)).ToArray();

            double[,] x = new double[ups.Length / 16 + downs.Length / 16, 16];
            int[] y = new int[ups.Length / 16 + downs.Length / 16];
            for (int i = 0; i < ups.Length / 16; i++)
            {
                for (int j = 0; j < 16; j++)
                    x[i, j] = ups[i * 16 + j];
                y[i] = 0;
            }

            for (int i = ups.Length; i < (ups.Length + downs.Length) / 16; i++)
            {
                for (int j = 0; j < 16; j++)
                    x[i, j] = downs[(i - ups.Length) * 16 + j];
                y[i] = 1;
            }
            int[][] Layers = new int[][] { new int[] { 16, 20, 2 }, new int[] { 16, 25, 15, 5, 2 }, new int[] { 16, 2 }, new int[] { 16, 3, 5, 2 } };
            NeuralNet n = NeuralNet.ChooseBestNeuralNet(x, y, Layers, 500);
            Console.WriteLine(n.PercentCorrect() + "% Correct");
            string layers = "";
            foreach (var i in n.Layers)
                layers += i + " ";
            Console.WriteLine("Layers: " + layers);
            return n;
        }

        static void EEGTestWithNN2()
        {
            EEG eeg = new EEG("COM3");
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
            EEGTestWithNN2();
            Console.ReadLine();
        }
    }
}
