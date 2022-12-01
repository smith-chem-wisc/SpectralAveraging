using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging.NoiseEstimates
{
    public static class ModWtOutputExtensions
    {
        /// <summary>
        /// Sums the wavelet coefficients over the ModWtOutput object. 
        /// </summary>
        /// <param name="output">A ModWtOutput object. </param>
        /// <returns>The summed wavelet coefficients.</returns>
        public static double[] SumWaveletCoefficients(this ModWtOutput output)
        {
            var waveletCoeffs = output.Levels.Select(i => i.WaveletCoeff);

            double[] summedResults = new double[waveletCoeffs.First().Length];
            for (int h = 0; h < waveletCoeffs.Count(); h++)
            {
                for (int i = 0; i < summedResults.Length; i++)
                {
                    summedResults[i] += waveletCoeffs.ElementAt(h).ElementAt(i);
                }
            }
            return summedResults;
        }
    }
}
