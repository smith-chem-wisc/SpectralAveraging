using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Integration;
using Nett;

namespace SpectralAveraging.NoiseEstimates
{
    public class WaveletMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="V">Original signal</param>
        /// <param name="N">Length of original signal</param>
        /// <param name="j">Current scale</param>
        /// <param name="h">Wavelet filter</param>
        /// <param name="g">Scaling filter</param>
        /// <param name="L">Length of filter</param>
        /// <param name="Wj">Wavelet coefficients out</param>
        /// <param name="Vj">Scaling coefficients out</param>
        public static void ModwtForward(double[] V, int N, int j,
            double[] h, double[] g, int L, ref double[] Wj, ref double[] Vj)
        {
            int t, k, n;
            double k_div;

            for (t = 0; t < N; t++)
            {
                k = t;
                Wj[t] = h[0] * V[k];
                Vj[t] = g[0] * V[k];
                for (n = 1; n < L; n++)
                {
                    k -= (int)Math.Pow(2, (j-1));
                    k_div = -(double)k / (double)N;
                    if (k < 0) k += (int)Math.Ceiling(k_div) * N;
                    Wj[t] += h[n] * V[k];
                    Vj[t] += g[n] * V[k]; 
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="waveletFilter"></param>
        /// <param name="scalingFilter"></param>
        /// <param name="numScales"></param>
        public static ModWtOutput ModWt(double[] signal, double[] waveletFilter, 
            double[] scalingFilter, WaveletType waveletType)
        {
            // calculate the number of scales to iterate over
            int numScales = (int)Math.Floor(Math.Log2(signal.Length));

            // use reflected boundary
            double[] reflectedSignal = CreateReflectedArray(signal); 

            var output = new ModWtOutput(numScales, waveletType, BoundaryType.Reflection);
            double[] waveletCoeffs = new double[reflectedSignal.Length];
            double[] scalingCoeffs = new double[reflectedSignal.Length];
            for (int i = 0; i < numScales; i++)
            {
                ModwtForward(reflectedSignal, reflectedSignal.Length, i + 1, waveletFilter,
                    scalingFilter, waveletFilter.Length, ref waveletCoeffs, 
                    ref scalingCoeffs); 
                output.AddLevel(waveletCoeffs, scalingCoeffs, i, BoundaryType.Reflection, signal.Length, waveletFilter.Length); 
            }
            return output; 
        }

        public static double[] CreateReflectedArray(double[] original)
        {
            double[] reflectedArray = new double[original.Length*2];
            double[] copyOfOriginal = new double[original.Length];

            // copy original into new
            Buffer.BlockCopy(original, 0, copyOfOriginal, 0, sizeof(double)*copyOfOriginal.Length);
            // reverse copy of the original array
            Array.Reverse(copyOfOriginal);
            // Combine the original and the reverse arrays 
            Buffer.BlockCopy(original, 0, reflectedArray, 0, original.Length*sizeof(double));

            int reverseArrayOffset = sizeof(double) * (copyOfOriginal.Length); 
            // original array is copied starting at element zero
            Buffer.BlockCopy(copyOfOriginal, 0, 
                // dst array contains the original signal, so need to offset the copying 
                // of the reflected signal. 
                reflectedArray, reverseArrayOffset, 
                copyOfOriginal.Length * sizeof(double));
            return reflectedArray; 

        }

        public static ModWtOutput ModWt(double[] signal, WaveletFilter filters)
        {
            return ModWt(signal, filters.WaveletCoefficients, filters.ScalingCoefficients, filters.WaveletType);
        }
    }

    public enum WaveletType
    {
        Haar = 1
    }

    public enum BoundaryType
    {
        Reflection = 1
    }

    public class ModWtOutput
    {
        public ModWtOutput(int maxScale, WaveletType waveletType, BoundaryType boundaryType)
        {
            Levels = new List<Level>(); 
            MaxScale = maxScale;
            WaveletType = waveletType;
            BoundaryType = boundaryType; 
        }

        public List<Level> Levels { get; private set; }
        public int MaxScale { get; private set; }
        public WaveletType WaveletType { get; }
        public BoundaryType BoundaryType { get; }
        public void AddLevel(Level level)
        {
            Levels.Add(level);
        }

        public void AddLevel(double[] waveletCoeff, double[] scalingCoeff, int scale, 
            BoundaryType boundaryType, int originalSignalLength, int filterLength)
        {
            if (boundaryType == BoundaryType.Reflection)
            {
                int startIndex = ((int)Math.Pow(2, scale-1))*(filterLength - 1);
                int stopIndex = startIndex + originalSignalLength; 
                Levels.Add(new Level(scale, 
                    waveletCoeff[startIndex .. stopIndex], 
                    scalingCoeff[startIndex .. stopIndex]));
            }
        }

        public void PrintToTxt(string path)
        {
            StringBuilder sb = new();
            foreach (var level in Levels)
            {
                sb.AppendLine(string.Join("\t", level.WaveletCoeff)); 
                sb.AppendLine(string.Join("\t", level.ScalingCoeff));
            }
            File.WriteAllText(path, sb.ToString());
        }
    }

    public class Level
    {
        public Level(int scale, double[] waveletCoeff, double[] scalingCoeff)
        {
            Scale = scale; 
            WaveletCoeff = waveletCoeff;
            ScalingCoeff = scalingCoeff;
        }

        public Level(int scale)
        {
            Scale = scale; 
        }

        public int Scale { get; private set; } 
        public double[] WaveletCoeff { get; private set; }
        public double[] ScalingCoeff { get; private set; }

    }

    public class WaveletFilter
    {
        public double[] WaveletCoefficients { get; private set; }
        public double[] ScalingCoefficients { get; private set; }
        public WaveletType WaveletType { get; private set; }

        public void CreateFiltersFromCoeffs(double[] filterCoeffs)
        {
            WaveletCoefficients = new double[filterCoeffs.Length];
            ScalingCoefficients = new double[filterCoeffs.Length]; 

            // calculate wavelet coefficients
            for (int i = 0; i < ScalingCoefficients.Length; i++)
            {
                ScalingCoefficients[i] = filterCoeffs[i] / Math.Sqrt(2d); 
            }
            WaveletCoefficients = WaveletMathUtils.QMF(ScalingCoefficients, inverse: true); 
        }

        public void CreateFiltersFromCoeffs(WaveletType waveletType)
        {
            switch (waveletType)
            {
                case WaveletType.Haar:
                {
                    WaveletType = WaveletType.Haar; 
                    CreateFiltersFromCoeffs(_haarCoefficients);
                    return; 
                }
            }
        }
        private readonly double[] _haarCoefficients =
        {
            0.7071067811865475,
            0.7071067811865475
        }; 
    }

    public static class WaveletMathUtils
    {
        /// <summary>
        /// Calcualte the quadruture mirror filter for x, an array of filter coefficients
        /// </summary>
        /// <param name="x"></param>
        /// <param name="inverse"></param>
        /// <returns></returns>
        public static double[] QMF(double[] x, bool inverse = false)
        {
            double[] y = new double[x.Length];
            Buffer.BlockCopy(x, 0, y, 0, x.Length * sizeof(double));
            Array.Reverse(y);
            if (inverse)
            {
                // start the for loop from the back
                int firstIndex = 1; 
                for (int i = x.Length - 1; i >= 0; i--)
                {
                    y[i] *= Math.Pow(-1d, firstIndex);
                    firstIndex++;
                }
            }
            else
            {
                for (int i = 0; i < x.Length; i++)
                {
                    y[i] *= Math.Pow(-1d, i + 1);
                }
            }
            return y; 
        }
    }
}
