using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging
{
    internal class BasicStatistics
    {
        /// <summary>
        /// Calculates the median of a list of doubles
        /// </summary>
        /// <param name="toCalc">initial list to calculate from</param>
        /// <returns>double representation of the median</returns>
        internal static double CalculateMedian(IEnumerable<double> toCalc)
        {
            IEnumerable<double> sortedValues = toCalc.OrderByDescending(p => p);
            double median;
            int count = sortedValues.Count();
            if (count % 2 == 0)
                median = sortedValues.Skip(count / 2 - 1).Take(2).Average();
            else
                median = sortedValues.ElementAt(count / 2);
            return median;
        }

        internal static double CalculateNonZeroMedian(IEnumerable<double> toCalc)
        {
            toCalc = toCalc.Where(p => p != 0);
            if (toCalc.Count() == 0)
                return 0;
            else
            {
                return CalculateMedian(toCalc.ToList());
            }
        }

        /// <summary>
        /// Calculates the standard deviation of a list of doubles
        /// </summary>
        /// <param name="toCalc">initial list to calculate from</param>
        /// <param name="average">passable value for the average</param>
        /// <returns>double representation of the standard deviation</returns>
        internal static double CalculateStandardDeviation(IEnumerable<double> toCalc, double average = 0)
        {
            double deviation = 0;

            if (toCalc.Any())
            {
                average = average == 0 ? toCalc.Average() : average;
                double sum = toCalc.Sum(x => Math.Pow(x - average, 2));
                deviation = Math.Sqrt(sum / toCalc.Count() - 1);
            }
            return deviation;
        }

        internal static double CalculateStandardDeviation(double[] toCalc, double average = 0)
        {
            return CalculateStandardDeviation((IEnumerable<double>)toCalc, average);
        }

        internal static double CalculateNonZeroStandardDeviation(IEnumerable<double> toCalc, double average = 0)
        {
            toCalc = toCalc.Where(p => p != 0);
            if (toCalc.Count() == 0)
                return 0;
            else
                return CalculateStandardDeviation(toCalc, average);
        }
    }
}
