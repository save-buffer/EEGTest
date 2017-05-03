using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EEGTest
{
    class NeuralNet
    {
        //TODO(sasha): Optimize this. I'm sure there are many performance improvements to be made.
        double lambda = 0.5;
        const double epsilon = 0.12;
        Matrix<double>[] Theta;
        Matrix<double> X;
        Matrix<double> CV;
        Matrix<double> Test;
        Vector<double> Y;
        Vector<double> CVY;
        Vector<double> TestY;
        public readonly int[] Layers;
        int num_labels;

        public int Predict(double[] x)
        {
            if (x.Length != Layers[0])
                throw new Exception("The input layer requires " + Layers[0] + " values. You gave " + x.Length + ".");
            return Predict(CreateVector.DenseOfArray(x));
        }

        int Predict(Vector<double> x)
        {
            if (x.Count != Layers[0])
                throw new Exception("The input layer requires " + Layers[0] + " values. You gave " + x.Count + ".");
            var _x = x.ToRowMatrix();
            Matrix<double> prev_activation = _x.Transpose(); //0th (Input) Layer's activation
            prev_activation = MathNetHelpers.AddRowOfOnes(prev_activation);
            for (int i = 1; i < Layers.Length - 1; i++)
            {
                var z = Theta[i - 1] * prev_activation;
                prev_activation = MathNetHelpers.AddRowOfOnes(MathNetHelpers.Sigmoid(z));
            }
            var _z = Theta[Theta.Length - 1] * prev_activation;
            prev_activation = MathNetHelpers.Sigmoid(_z);
            int max_index = 0;
            for (int i = 0; i < prev_activation.Column(0).Count; i++)
                if (prev_activation[i, 0] > prev_activation[max_index, 0])
                    max_index = i;
            return max_index;
        }

        Matrix<double> Hypothesis()
        {
            Matrix<double> prev_activation = X.Transpose(); //0th (Input) Layer's activationk
            prev_activation = MathNetHelpers.AddRowOfOnes(prev_activation);
            for (int i = 1; i < Layers.Length - 1; i++)
            {
                var z = Theta[i - 1] * prev_activation;
                prev_activation = MathNetHelpers.AddRowOfOnes(MathNetHelpers.Sigmoid(z));
            }
            var _z = Theta[Theta.Length - 1] * prev_activation;
            prev_activation = MathNetHelpers.Sigmoid(_z);
            return prev_activation;
        }

        Matrix<double> Y_Matrix()
        {
            var a = X.ToArray();
            int m = a.GetUpperBound(0) + 1;
            Matrix<double> y_matrix = MathNetHelpers.Zeros(m, num_labels);
            for (int i = 0; i < m; i++)
            {
                y_matrix[i, (int)(Y[i] + 0.5)] = 1.0;
            }
            return y_matrix;
        }

        Matrix<double>[] Gradients()
        {
            var a = X.ToArray();
            int m = a.GetUpperBound(0) + 1;
            Matrix<double>[] grad = new Matrix<double>[Theta.Length];
            for (int i = 0; i < grad.Length; i++)
            {
                grad[i] = MathNetHelpers.Zeros(Theta[i].RowCount, Theta[i].ColumnCount);
            }

            Matrix<double>[] activations = new Matrix<double>[Layers.Length];
            Matrix<double> prev_activation = X.Transpose(); //0th (Input) Layer's activationk
            prev_activation = MathNetHelpers.AddRowOfOnes(prev_activation);
            activations[0] = prev_activation;
            for (int i = 1; i < Layers.Length - 1; i++)
            {
                var z = Theta[i - 1] * prev_activation;
                prev_activation = MathNetHelpers.AddRowOfOnes(MathNetHelpers.Sigmoid(z));
                activations[i] = prev_activation;
            }
            var _z = Theta[Theta.Length - 1] * prev_activation;
            prev_activation = MathNetHelpers.Sigmoid(_z);
            activations[activations.Length - 1] = prev_activation;

            Matrix<double>[] delta = new Matrix<double>[Layers.Length];
            delta[delta.Length - 1] = (prev_activation - Y_Matrix().Transpose()); //The delta of the output layer
            for (int i = delta.Length - 2; i > 0; i--)
            {
                var temp = MathNetHelpers.RemoveColumn(activations[i]);
                temp = MathNetHelpers.AddColumnOfOnes(temp);
                delta[i] = (Theta[i].Transpose() * delta[i + 1]).PointwiseMultiply(temp).PointwiseMultiply(1 - temp);
                delta[i] = MathNetHelpers.RemoveRow(delta[i]);
            }
            Matrix<double>[] Delta = new Matrix<double>[Theta.Length];
            for (int i = 0; i < Delta.Length; i++)
            {
                Delta[i] = (delta[i + 1] * activations[i].Transpose());
            }
            for (int i = 0; i < grad.Length; i++)
            {
                var z = MathNetHelpers.RemoveRow(Theta[i]);
                z = MathNetHelpers.AddRowOf(z, 0);
                grad[i] = (Delta[i] + lambda * z) / m;
            }
            return grad;
        }

        public void GradientDescent(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var grad = Gradients();
                for (int j = 0; j < grad.Length; j++)
                    Theta[j] = Theta[j] - grad[j];
            }
        }

        public double[] GradientDescentWithHistory(int iterations)
        {
            double[] j_history = new double[iterations];
            for (int i = 0; i < iterations; i++)
            {
                j_history[i] = J();
                Console.WriteLine(Hypothesis());
                var grad = Gradients();
                for (int j = 0; j < grad.Length; j++)
                    Theta[j] = Theta[j] - grad[j];
            }
            return j_history;
        }

        double CV_J()
        {
            var x = X;
            var y = Y;
            X = CV;
            Y = CVY;
            var result = J();
            X = x;
            Y = y;
            return result;
        }

        double J()
        {
            var a = X.ToArray();
            int m = a.GetUpperBound(0) + 1;
            Matrix<double> tmp = MathNetHelpers.Eye(num_labels);
            Matrix<double> y_matrix = Y_Matrix().Transpose();

            var h = Hypothesis();

            double regularization = 0;
            for (int i = 0; i < Theta.Length; i++)
                regularization += MathNetHelpers.ColumnRange(Theta[i].PointwisePower(2), 0, Theta[i].ColumnCount - 1).ColumnSums().Sum();
            regularization *= (lambda / (2 * m));

            double j = (-y_matrix.PointwiseMultiply(h.PointwiseLog()) - (1 - y_matrix).PointwiseMultiply(1 - h.PointwiseLog())).ColumnSums().Sum();
            return j + regularization;
        }

        public void TrainingSet(double[,] x, int[] y)
        {
            if (x.GetUpperBound(0) + 1 < y.Length)
                throw new Exception("You have more labels than training examples!");
            if (x.GetUpperBound(0) + 1 > y.Length)
                throw new Exception("You have more training examples than labels!");

            int cv_size = (int)(0.2 * (x.GetUpperBound(0) + 1));
            double[] _y = new double[y.Length - 2 * cv_size];

            y = (int[])y.Clone();
            X = CreateMatrix.DenseOfArray(x);

            Random r = new Random();
            for (int i = 0; i < X.RowCount - 2; i++)
            {
                int j = r.Next(i, X.RowCount);
                var a = X.Row(i);
                var b = X.Row(j);
                X.SetRow(j, a);
                X.SetRow(i, b);

                int t = y[i];
                y[i] = y[j];
                y[j] = t;
            }

            CV = CreateMatrix.DenseOfRowVectors(X.Row(0));
            X = X.RemoveRow(0);
            Test = CreateMatrix.DenseOfRowVectors(X.Row(0));
            X = X.RemoveRow(0);
            for (int i = 0; i < cv_size - 1; i++)
            {
                CV = CV.InsertRow(i + 1, X.Row(0));
                X = X.RemoveRow(0);
                Test = Test.InsertRow(i + 1, X.Row(0));
                X = X.RemoveRow(0);
            }
            double[] _cvy = new double[cv_size];
            double[] _testy = new double[cv_size];
            for (int i = 0; i < 2 * cv_size; i++)
            {
                _cvy[i / 2] = y[i++];
                _testy[i / 2] = y[i];
            }
            for (int i = 2 * cv_size; i < y.Length; i++)
                _y[i - 2 * cv_size] = y[i];
            Y = CreateVector.DenseOfArray(_y);
            CVY = CreateVector.DenseOfArray(_cvy);
            TestY = CreateVector.DenseOfArray(_testy);
        }

        public double PercentCorrect()
        {
            int correct = 0;
            for (int i = 0; i < Test.RowCount; i++)
            {
                if (Predict(Test.Row(i)) == TestY[i])
                    correct++;
            }
            return (double)correct / Test.RowCount * 100.0;
        }

        public static NeuralNet ChooseBestNeuralNet(double[,] x, int[] y, int[][] Layers, int GradientDescentIterations, double[] Regularization = null)
        {
            if (Regularization != null)
                if (Layers.Length != Regularization.Length)
                    throw new Exception("The number of layers to try does not match the number of regularizations to try!");
            NeuralNet min_net = null;
            double min_j = double.PositiveInfinity;
            NeuralNet a = new NeuralNet(new int[] { 1, 1, 1 });
            a.TrainingSet(x, y);
            for (int i = 0; i < Layers.Length; i++)
            {
                NeuralNet n = new NeuralNet(Layers[i], Regularization != null ? Regularization[i] : 0.1);
                n.X = a.X;
                n.Y = a.Y;
                n.CV = a.CV;
                n.CVY = a.CVY;
                n.Test = a.Test;
                n.TestY = a.TestY;
                n.GradientDescent(GradientDescentIterations);
                double j = n.CV_J();
                if (j < min_j)
                {
                    min_net = n;
                    min_j = j;
                }
            }
            return min_net;
        }

        //NOTE(sasha): Layers is the number of nodes in each layer. Layers does NOT include the bias nodes, includes the output layer
        public NeuralNet(int[] Layers, double Regularization = 0.1)
        {
            this.lambda = Regularization;
            this.Layers = Layers;
            num_labels = Layers[Layers.Length - 1];
            Theta = new Matrix<double>[Layers.Length - 1];
            for (int i = 0; i < Layers.Length - 1; i++)
            {
                Theta[i] = MathNetHelpers.RandomMatrix(Layers[i + 1], Layers[i] + 1, epsilon);
            }
        }
    }
}
