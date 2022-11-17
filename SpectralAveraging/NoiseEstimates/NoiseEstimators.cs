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

        public static void MSRNoiseEstimation(double[] signal, double epsilon )
        {
            // 1. Estimate the standard deviation of the noise in the original signal. 
            // 2. Compute the modwt of the image
            // 3. Set n = 0; 
            // 4. Compute the multiresolution support M that is derived from the wavelet coefficients
            // and the standard deviation of the noise at each level. 
            // 5. Select all points that belong to the noise; they don't have an significant coefficients above noise 
            // 6. For the selected pixels, calculate original signal - residual signal and compute the standard deviation 
            // for those values. 
            // 7. n = n + l. 
            // 8. start again at 4 if sigma_I^n - sigma_I^(n-1) / sigma_I^(n) > epsilon. 


        }

        public static void DetectLevel(Level level, double[] signal)
        {

        }
        

    }
}
