using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace KLib
{
    public class KMath
    {
        public static float Three_dB = 20 * Mathf.Log10(Mathf.Sqrt(2));

        public static float Log2(float value)
        {
            return Mathf.Log(value) / Mathf.Log(2);
        }

        public static float Min(float[] data)
        {
            float val = float.PositiveInfinity;
            for (int k = 0; k < data.Length; k++)
            {
                if (data[k] < val) val = data[k];
            }
            return val;
        }

        public static float Max(float[] data)
        {
            float val = float.NegativeInfinity;
            for (int k = 0; k < data.Length; k++)
            {
                if (data[k] > val) val = data[k];
            }
            return val;
        }

        public static int Max(params int[] data)
        {
            int val = int.MinValue;
            for (int k = 0; k < data.Length; k++)
            {
                if (data[k] > val) val = data[k];
            }
            return val;
        }

        public static float MaxAbs(float[] data)
        {
            float val = float.NegativeInfinity;
            for (int k = 0; k < data.Length; k++)
            {
                if (Mathf.Abs(data[k]) > val) val = Mathf.Abs(data[k]);
            }
            return val;
        }

        public static float Mean(float[] data)
        {
            float s = 0;
            for (int k = 0; k < data.Length; k++)
            {
                s += data[k];
            }
            return s / (float)data.Length;
        }

        public static float Median(float[] data)
        {
            float med = float.NaN;

            Array.Sort(data);
            if (data.Length % 2 == 1)
            {
                int i = Mathf.FloorToInt(data.Length / 2f) + 1;
                med = data[i];
            }
            else
            {
                int i = Mathf.FloorToInt(data.Length / 2f);
                med = 0.5f * (data[i] + data[i+1]);
            }

            return med;
        }

        public static float StdDev(float[] data)
        {
            float mean = Mean(data);
            float ss = 0;
            for (int k = 0; k < data.Length; k++)
            {
                float delta = data[k] - mean;
                ss += delta * delta;
            }
            return Mathf.Sqrt(ss / (float)data.Length);
        }

        public static float CoeffVar(float[] data)
        {
            return StdDev(data) / Mean(data);
        }

        public static float GeoMean(float[] data)
        {
            float s = 0;
            for (int k = 0; k < data.Length; k++)
            {
                s += Mathf.Log(data[k]);
            }
            return Mathf.Exp(s / (float)data.Length);
        }

        public static float MeanSquare(float[] data)
        {
            return SumOfSquares(data) / data.Length;
        }

        public static float SumOfSquares(float[] data)
        {
            float ss = 0;
            for (int k = 0; k < data.Length; k++)
            {
                ss += data[k] * data[k];
            }
            return ss;
        }

        public static float RMS(float[] data)
        {
            return Mathf.Sqrt(MeanSquare(data));
        }

        public static float RMS_dB(float[] data)
        {
            return 20f * Mathf.Log10(RMS(data));
        }

        public static float[] Unique(float[] data)
        {
            List<float> tmp = new List<float>();
            foreach (float f in data)
            {
                if (tmp.FindIndex(o => o == f) < 0) tmp.Add(f);
            }
            return tmp.ToArray();
        }

        public static bool IsEven(int val)
        {
            return val == 2 * Mathf.FloorToInt((float)val / 2f);
        }

        public static bool IsMultipleOf(int val, int root)
        {
            return (val - root * Mathf.FloorToInt((float)val / (float)root) == 0);
        }

        public static float OctaveRatio(float a, float b)
        {
            return Mathf.Log(a / b) / Mathf.Log(2);
        }

        public static float WrapAngle(float val)
        {
            return val - 360f * Mathf.Floor(val / 360f);
        }

        public static float MinAngle(float angle)
        {
            if (angle > 180)
                angle -= 360;
            if (angle < -180)
                angle += 360;
            return angle;
        }

        public static float Magnitude(float a, float b)
        {
            return (Mathf.Sqrt(a * a + b * b));
        }

        public static float MagnitudeSqr(float a, float b)
        {
            return (a * a + b * b);
        }

        /// <summary>
        /// Absolute angle of vector in XY-plane
        /// </summary>
        /// <returns>The angle.</returns>
        /// <param name="a">Vector.</param>
        public static float SignedAngle(Vector3 a)
        {
            float angle = Vector3.Angle(a, Vector3.right);
            float sign = Mathf.Sign(Vector3.Cross(Vector3.right, a).z);
            return angle * sign;
        }

        public static float SignedAngleXY(Vector3 a)
        {
            a.z = 0;
            return SignedAngle(a);
        }

        public static int[] Permute(int N, int numElements)
        {
            int nrepeats = Mathf.CeilToInt((float)numElements / N);
            int[] list = new int[nrepeats * N];
            int idx = 0;
            for (int k=0; k<nrepeats; k++)
            {
                foreach (int i in Permute(N)) list[idx++] = i;
            }

            int[] trimmed = new int[numElements];
            for (int k = 0; k < numElements; k++) trimmed[k] = list[k];

            return trimmed;
        }

        public static int[] SetDiff(int[] A, int[] B)
        {
            var listB = new List<int>(B);

            List<int> keep = new List<int>();

            for (int k=0; k < A.Length; k++)
            {
                if (!listB.Contains(A[k]))
                {
                    keep.Add(A[k]);
                }
            }

            return keep.ToArray();
        }

        public static int Seed
        {
            get { return UnityEngine.Random.seed; }
            set { UnityEngine.Random.seed = value; }
        }

        public static int[] Permute(int N)
        {
            int[] list = new int[N];
            for (int k = 0; k < N; k++)
                list[k] = k;

            int max = N - 1;
            for (int k = 0; k < N; k++)
            {
                int idx = UnityEngine.Random.Range(0, max + 1);
                int temp = list[idx];
                list[idx] = list[max];
                list[max] = temp;
                --max;
            }

            return list;
        }

        public static int[] LinSpace(int N)
        {
            int[] val = new int[N];
            for (int k = 0; k < N; k++) val[k] = k;
            return val;
        }

        public static float[] Permute(float[] y)
        {
            float[] x = new float[y.Length];
            int[] newOrder = Permute(y.Length);
            for (int k = 0; k < y.Length; k++)
                x[k] = y[newOrder[k]];

            return x;
        }

        public static int[] Permute(int[] y)
        {
            int[] x = new int[y.Length];
            int[] newOrder = Permute(y.Length);
            for (int k = 0; k < y.Length; k++)
                x[k] = y[newOrder[k]];

            return x;
        }

        public static float Interp1(float[] X, float[] Y, float U)
        {
            float V = float.NaN;

            int tableIndex = X.Length;
            for (int k = 0; k < X.Length; k++)
            {
                if (X[k] > U)
                {
                    tableIndex = k;
                    break;
                }
            }

            if (tableIndex == 0)
            {
                V = Y[0];
            }
            else if (tableIndex == X.Length)
            {
                V = Y[X.Length - 1];
            }
            else if (X[tableIndex - 1] == U)
            {
                V = Y[tableIndex - 1];
            }
            else
            {
                float f0 = X[tableIndex - 1];
                float f1 = X[tableIndex];
                V = (U - f0) / (f1 - f0) * (Y[tableIndex] - Y[tableIndex - 1]) + Y[tableIndex - 1];
            }

            return V;
        }

        public static float[] Interp1(float[] X, float[] Y, float du, int npts)
        {
            float[] U = new float[npts];
            for (int k = 0; k < npts; k++) U[k] = k * du;

            return Interp1(X, Y, U);
        }

        public static float[] Interp1(float[] X, float[] Y, float[] U)
        {
            float[] V = new float[U.Length];

            int tableIndex = 0;
            for (int k = 0; k < U.Length; k++)
            {
                if (tableIndex < X.Length && U[k] > X[tableIndex])
                {
                    ++tableIndex;
                }

                if (tableIndex == 0)
                {
                    V[k] = Y[0];
                }
                else if (tableIndex == X.Length)
                {
                    V[k] = Y[X.Length - 1];
                }
                else
                {
                    float f0 = X[tableIndex - 1];
                    float f1 = X[tableIndex];
                    V[k] = (U[k] - f0) / (f1 - f0) * (Y[tableIndex] - Y[tableIndex - 1]) + Y[tableIndex - 1];
                }
            }
            return V;
        }

        public static float RandomSign()
        {
            return UnityEngine.Random.Range((int)0, (int)2) == 0 ? -1 : 1;
        }

    }
}