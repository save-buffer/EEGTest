using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace EEGTest
{
    class MathNetHelpers
    {
        public static Matrix<double> Eye(int n)
        {
            return CreateMatrix.DiagonalIdentity<double>(n);
        }

        public static Matrix<double> Zeros(int m, int n)
        {
            double[,] arr = new double[m, n];
            return CreateMatrix.DenseOfArray<double>(arr);
        }

        public static Matrix<double> Ones(int m, int n)
        {
            double[,] arr = new double[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    arr[i, j] = 1.0;
            return CreateMatrix.DenseOfArray<double>(arr);
        }

        public static Matrix<double> RemoveRow(Matrix<double> x)
        {
            return RowRange(x, 1, x.RowCount - 1);
        }

        public static Matrix<double> RemoveColumn(Matrix<double> x)
        {
            return ColumnRange(x, 1, x.ColumnCount - 1);
        }

        public static Matrix<double> Sigmoid(Matrix<double> x)
        {
            var a = x.ToArray();
            for (int i = 0; i <= a.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= a.GetUpperBound(1); j++)
                {
                    a[i, j] = 1 / (1 + Math.Exp(-a[i, j]));
                }
            }
            return CreateMatrix.DenseOfArray<double>(a);
        }

        public static Matrix<double> SigmoidGradient(Matrix<double> X)
        {
            var sig = Sigmoid(X);
            return sig.PointwiseMultiply(1 - sig);
        }

        public static Matrix<double> RowRange(Matrix<double> X, int min, int max)
        {
            double[,] arr = new double[max - min + 1, X.ColumnCount];
            for (int i = 0; i <= arr.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= arr.GetUpperBound(1); j++)
                {
                    arr[i, j] = X[min + i, j];
                }
            }
            return CreateMatrix.DenseOfArray<double>(arr);
        }

        public static Matrix<double> ColumnRange(Matrix<double> X, int min, int max)
        {
            double[,] arr = new double[X.RowCount, max - min + 1];
            for (int i = 0; i <= arr.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= arr.GetUpperBound(1); j++)
                {
                    arr[i, j] = X[i, min + j];
                }
            }
            return CreateMatrix.DenseOfArray<double>(arr);
        }

        public static Matrix<double> AddRowOfOnes(Matrix<double> X)
        {
            double[,] arr = new double[X.RowCount + 1, X.ColumnCount];
            for (int i = 0; i <= arr.GetUpperBound(0); i++) //rows
            {
                for (int j = 0; j <= arr.GetLowerBound(1); j++) //columns
                {
                    if (i == 0)
                        arr[i, j] = 1.0;
                    else
                        arr[i, j] = X[i - 1, j];
                }
            }
            return CreateMatrix.DenseOfArray(arr);
        }

        public static Matrix<double> AddRowOf(Matrix<double> X, double n)
        {
            double[,] arr = new double[X.RowCount + 1, X.ColumnCount];
            for (int i = 0; i <= arr.GetUpperBound(0); i++) //rows
            {
                for (int j = 0; j <= arr.GetLowerBound(1); j++) //columns
                {
                    if (i == 0)
                        arr[i, j] = n;
                    else
                        arr[i, j] = X[i - 1, j];
                }
            }
            return CreateMatrix.DenseOfArray(arr);
        }

        public static Matrix<double> AddColumnOfOnes(Matrix<double> X)
        {
            double[,] arr = new double[X.RowCount, X.ColumnCount + 1];
            for (int i = 0; i <= arr.GetUpperBound(0); i++) //rows
            {
                for (int j = 0; j <= arr.GetLowerBound(1); j++) //columns
                {
                    if (j == 0)
                        arr[i, j] = 1.0;
                    else
                        arr[i, j] = X[i, j - 1];
                }
            }
            return CreateMatrix.DenseOfArray(arr);
        }

        public static Matrix<double> AddColumnOf(Matrix<double> X, double n)
        {
            double[,] arr = new double[X.RowCount, X.ColumnCount + 1];
            for (int i = 0; i <= arr.GetUpperBound(0); i++) //rows
            {
                for (int j = 0; j <= arr.GetLowerBound(1); j++) //columns
                {
                    if (j == 0)
                        arr[i, j] = n;
                    else
                        arr[i, j] = X[i, j - 1];
                }
            }
            return CreateMatrix.DenseOfArray(arr);
        }

        public static Matrix<double> RandomMatrix(int m, int n, double epsilon)
        {
            Random r = new Random();
            double[,] arr = new double[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    arr[i, j] = r.NextDouble() * 2 * epsilon - epsilon;
            return CreateMatrix.DenseOfArray<double>(arr);
        }
    }
}
