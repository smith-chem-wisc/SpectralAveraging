using System;
using System.Collections.Generic;
using System.Data;
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

        public static double MRSNoiseEstimation(double[] signal, double epsilon, int maxIterations = 25)
        {
            int iterations = 0; 
            // 1. Estimate the standard deviation of the noise in the original signal. 
            double stdevPrevious = BasicStatistics.CalculateStandardDeviation(signal);

            // 2. Compute the modwt of the image
            WaveletFilter filter = new(); 
            filter.CreateFiltersFromCoeffs(WaveletType.Haar);
            ModWtOutput wtOutput = WaveletMath.ModWt(signal, filter);

            // 3. Set n = 0; 
            int n = 0;

            double[] signalIterable = new double[signal.Length]; 
            signal.CopyTo(signalIterable, 0);

            double criticalVal = 0d;
           
            signalIterable = CreateSmoothedSignal(signalIterable, wtOutput);
            double stdevNext = stdevPrevious;
            do
            {
                // 4. Compute the multiresolution support M that is derived from the wavelet coefficients
                // and the standard deviation of the noise at each level. 
                // 5. Select all points that belong to the noise; they don't have an significant coefficients above noise 
                var booleanizedLevels = ModWtOuputExtensions.BooleanizeLevels(wtOutput,
                    stdevPrevious, 1.97); 
                int[] mrsIndices = CreateMultiResolutionSupport(booleanizedLevels);

                // 6. For the selected pixels, calculate original array - smoothed array and compute the standard deviation 
                // for those values. 
                // don't modify the original signal, use a deep copy instead: 
                stdevNext = wtOutput.ComputeStdevOfNoisePixels(signalIterable, mrsIndices);

                // 7. n = n + l. 
                // 8. start again at 4 if sigma_I^n - sigma_I^(n-1) / sigma_I^(n) > epsilon. 
                criticalVal = Math.Abs(stdevNext - stdevPrevious) / stdevPrevious;

                // setup for next iteration 
                iterations++; 
                stdevPrevious = stdevNext;
            } while (criticalVal > epsilon && iterations <= maxIterations);

            return stdevNext; 
        }

        public static double[] CreateSmoothedSignal(double[] originalSignal, ModWtOutput output)
        {
            double[] results = new double[originalSignal.Length];
            double[] summedWt = output.SumWaveletCoefficients();

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = originalSignal[i] - summedWt[i]; 
            }

            return results; 
        }

        public static int[] CreateMultiResolutionSupport(List<int[]> booleanizedLevels)
        {
            int[] outputArray = new int[booleanizedLevels[0].Length];
            for (int i = 0; i < outputArray.Length; i++)
            {
                outputArray[i] = booleanizedLevels.Select(k => k.ElementAt(i)).Sum();
            }
            return outputArray;
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

        public static double ComputeStdevOfNoisePixels(this ModWtOutput wtOutput, double[] signal, int[] noiseIndices)
        {
            List<double> noiseValues = new();
            for (int k = 0; k < noiseIndices.Length; k++)
            {
                if (noiseIndices[k] == 0)
                {
                    noiseValues.Add(signal.ElementAt(k));
                }
                 
            }

            return BasicStatistics.CalculateStandardDeviation(noiseValues); 
        }

        //public static void CreateMultiResolutionSupport(this ModWtOutput wtOutput, double noiseEstimate,
        //    double noiseThreshold = 1.97)
        //{

        //    //List<int> indexList = new(); 
        //    //foreach (Level level in wtOutput.Levels)
        //    //{
        //    //    ValueFailsToExceedStd(level, noiseThreshold, noiseEstimate, ref indexList);
        //    //}
        //    //// get all distinct values 
        //    //return indexList.GroupBy(n => n)
        //    //    .Where(g => g.Count() > 1)
        //    //    .Select(g => g.Key).ToList();
        //}

        

        public static List<int[]> BooleanizeLevels(ModWtOutput wtOutput, double noiseEstimate, double noiseThreshold)
        {
            List<int[]> booleanizedLevels = new();
            foreach (Level level in wtOutput.Levels)
            {
                booleanizedLevels.Add(level.BooleanizeLevel(noiseEstimate, noiseThreshold));
            }
            return booleanizedLevels;
        }

        // need some sort of static data structure 
        private static void ModOutputToDataTable(ModWtOutput wtOutput)
        {

        }

        private static int[] BooleanizeLevel(this Level level, double noiseEstimate, double threshold)
        {
            int[] results = new int[level.WaveletCoeff.Length]; 
            double valToExceed = threshold * noiseEstimate;

            for (int i = 0; i < results.Length; i++)
            {
                if (Math.Abs(level.WaveletCoeff[i]) >= valToExceed)
                {
                    results[i] = 1;
                }
                else
                {
                    results[i] = 0; 
                }
            }
            return results; 
        }

        /// <summary>
        /// Returns the indexes of the array that contain noise. 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="noiseThreshold"></param>
        /// <returns></returns>
        private static void ValueFailsToExceedStd(this Level level, double noiseThreshold, double noiseEstimate, ref List<int> indexList)
        {
            List<int> valuesThatFailed = new();

            double threshold = noiseEstimate * noiseThreshold;
            for (int i = 0; i < level.WaveletCoeff.Length; i++)
            {
                 
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
