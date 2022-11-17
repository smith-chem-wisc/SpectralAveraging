using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using Nett;

namespace SpectralAveraging.NoiseEstimates
{
    public class WaveletMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="signal">Must be power of 2 length.</param>
        /// <param name="waveletFilter"></param>
        /// <param name="scalingFilter"></param>
        /// <param name="scales"></param>
        /// <param name="scalingCoeff"></param>
        /// <param name="waveletCoeff"></param>
        public static void ModwtForward(double[] signal, double[] waveletFilter,
            double[] scalingFilter, int scale, ref double[] scalingCoeff, 
            ref double[] waveletCoeff)
        {
            int n = signal.Length;

            int l = waveletFilter.Length;
            int d = (int)Math.Pow(2d, (double)(scale));
            int k = 0; 
            
            for (int t = 0; t < n; k = (++t))
            {
                scalingCoeff[t] = scalingFilter[0] * signal[t];
                waveletCoeff[t] = waveletFilter[0] * signal[t];

                for (int v = 1; v < l; ++v)
                {
                    if (k >= d)
                    {
                        k -= d;
                    }
                    else
                    {
                        k = n + k - d; 
                    }
                    scalingCoeff[t] += scalingFilter[v] * signal[(int)k];
                    waveletCoeff[t] += waveletFilter[v] * signal[(int)k]; 
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
        public static ModWtOutput ModWt(double[] signal, double[] waveletFilter, double[] scalingFilter, 
            WaveletType waveletType)
        {
            // calculate the number of scales to iterate over
            int numScales = (int)Math.Floor(Math.Log2(signal.Length)); 

            var output = new ModWtOutput(numScales, waveletType); 
            for (int i = 0; i < numScales; ++i)
            {
                double[] waveletCoeffs = new double[signal.Length];
                double[] scalingCoeffs = new double[signal.Length];
                ModwtForward(signal, waveletFilter, scalingFilter, i,
                    ref scalingCoeffs, ref waveletCoeffs);
                output.AddLevel(waveletCoeffs, scalingCoeffs, i); 
            }
            return output; 
        }

        public static ModWtOutput ModWt(double[] signal, WaveletFilter filters)
        {
            if (signal.Length != NoiseEstimators.ClosestPow2(signal.Length))
            {
                NoiseEstimators.PadZeroes(signal, out double[] paddedSignal);
                return ModWt(paddedSignal, filters.WaveletCoefficients, filters.ScalingCoefficients, filters.WaveletType);
            }
            return ModWt(signal, filters.WaveletCoefficients, filters.ScalingCoefficients, filters.WaveletType);
        }
    }

    public enum WaveletType
    {
        Haar = 1
    }

    public class ModWtOutput
    {
        public ModWtOutput(int maxScale, WaveletType waveletType)
        {
            Levels = new List<Level>(); 
            MaxScale = maxScale;
            WaveletType = waveletType;
        }

        public List<Level> Levels { get; private set; }
        public int MaxScale { get; private set; }
        public WaveletType WaveletType { get; }

        public void AddLevel(Level level)
        {
            Levels.Add(level);
        }

        public void AddLevel(double[] waveletCoeff, double[] scalingCoeff, int scale)
        {
            Levels.Add(new Level(scale, waveletCoeff, scalingCoeff));
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
