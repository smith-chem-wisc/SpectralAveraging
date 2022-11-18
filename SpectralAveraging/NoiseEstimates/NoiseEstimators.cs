using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging.NoiseEstimates
{
    public class NoiseEstimators
    {
        public static void MRSNoiseEstimate()
        {

        }

        public static void PadZeroes(double[] signal, out double[] paddedSignal)
        {
            int paddedLength = ClosestPow2(signal.Length); 
            paddedSignal = new double[paddedLength];
            // copies the original data to the new, padded buffer. 
            // Remember that arrays are initialized with zeroes 
            int bytesToCopy = sizeof(double) * signal.Length; 
            Buffer.BlockCopy(signal, 0, 
                paddedSignal, 0, bytesToCopy);
        }
        /// <summary>
        /// Returns the closest power of 2 of a 32-bit integer. 
        /// </summary>
        /// <param name="val"> Must only be positive integer, typically the length of a particular signal.</param>
        /// <returns></returns>
        public static int ClosestPow2(int val)
        {
            // Value passed here should always be a positive integer, 
            // so initially cast to a 32-bit unsigned int. 
            // Algorithm from the http://graphics.stanford.edu/%7Eseander/bithacks.html#RoundUpPowerOf2
            uint v = (uint)val;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return (int)v;
        }

        public static void MSRNoiseEstimation(double[] signal, double epsilon)
        {
            // 1. Estimate the standard deviation of the noise in the original signal. 
            double stdevInitial = BasicStatistics.CalculateStandardDeviation(signal);

            // 2. Compute the modwt of the image
            WaveletFilter filter = new(); 
            filter.CreateFiltersFromCoeffs(WaveletType.Haar);
            ModWtOutput wtOutput = WaveletMath.ModWt(signal, filter);

            double[] lowFreqComponent = CalculateLowFrequencyComponent(signal, wtOutput); 
            // 3. Set n = 0; 
            int n = 0;
            // 4. Compute the multiresolution support M that is derived from the wavelet coefficients
            // and the standard deviation of the noise at each level. 
            // 5. Select all points that belong to the noise; they don't have an significant coefficients above noise 
            List<int> mrsIndices = wtOutput.CreateMultiResolutionSupport(stdevInitial);
            
            // 6. For the selected pixels, calculate original signal - residual signal and compute the standard deviation 
            // for those values. 


            // 7. n = n + l. 
            // 8. start again at 4 if sigma_I^n - sigma_I^(n-1) / sigma_I^(n) > epsilon. 


        }

        public static double[] CalculateLowFrequencyComponent(double[] originalSignal, ModWtOutput output)
        {
            double[] results = new double[originalSignal.Length];
            double[] summedWt = output.SumWaveletCoefficients();

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = originalSignal[i] + summedWt[i]; 
            }

            return results; 
        }

        
    }

    public static class ModWtOuputExtensions
    {
        public static Dictionary<int, double> GetSumOfWaveletCoeffsFromNoise(this ModWtOutput output, List<int> noiseIndices)
        {
            var waveletCoeffs = output.Levels.Select(i => i.WaveletCoeff).ToList();

            List<List<double>> listOfNoiseValsAtEachScale = new(); 
            for (int h = 0; h < waveletCoeffs.Count(); h++)
            {
                List<double> tempArrayOfNoiseVals = new();
                for (int k = 0; k < noiseIndices.Count(); k++)
                {
                    double tempVal = waveletCoeffs[h].ElementAt(noiseIndices.ElementAt(k)); 
                    tempArrayOfNoiseVals.Add(tempVal);
                }
                listOfNoiseValsAtEachScale.Add(tempArrayOfNoiseVals.ToList());
            }
            // calculate the stdev at each scale
            Dictionary<int, double> scaleStdevDictionary = new();
            int i = 1; 
            foreach (List<double> waveletVals in listOfNoiseValsAtEachScale)
            {
                scaleStdevDictionary.Add(i, BasicStatistics.CalculateStandardDeviation(waveletVals));
                i++; 
            }
            return scaleStdevDictionary;
        }

        public static double ComputeStdevOfNoisePixels(this ModWtOutput wtOutput, double[] signal, List<int> noiseIndices)
        {
            List<double> noiseValues = new();
            for (int k = 0; k < noiseIndices.Count; k++)
            {
                noiseValues.Add(signal.ElementAt(noiseIndices.ElementAt(k)));
            }

            return BasicStatistics.CalculateStandardDeviation(noiseValues); 
        }

        public static List<int> CreateMultiResolutionSupport(this ModWtOutput wtOutput, double noiseEstimate,
            int noiseThreshold = 3)
        {
            List<int> indexList = new(); 
            foreach (Level level in wtOutput.Levels)
            {
                ValueFailsToExceedStd(level, noiseThreshold, ref indexList);
            }
            // get all distinct values 
            return indexList.GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
        }


        /// <summary>
        /// Returns the indexes of the array that contain noise. 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="noiseThreshold"></param>
        /// <returns></returns>
        private static void ValueFailsToExceedStd(this Level level, int noiseThreshold, ref List<int> indexList)
        {
            List<int> valuesThatFailed = new();
            double stdevNoiseAtLevel = BasicStatistics.CalculateStandardDeviation(level.WaveletCoeff); 
            
            for (int i = 0; i < level.WaveletCoeff.Length; i++)
            {
                double threshold = stdevNoiseAtLevel * noiseThreshold; 
                if (level.WaveletCoeff[i] < threshold)
                {
                    indexList.Add(i);
                }
            }
        }

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
