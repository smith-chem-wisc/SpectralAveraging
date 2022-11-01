﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging
{
    public static class OutlierRejection
    {
        /// <summary>
        /// Calls the specific rejection function based upon the current static field RejectionType
        /// </summary>
        /// <param name="mzValues">list of mz values to evaluate<</param>
        /// <returns></returns>
        public static double[] RejectOutliers(double[] mzValues, SpectralAveragingOptions options)
        {
            double[] trimmedMzValues = mzValues;
            switch (options.RejectionType)
            {
                case RejectionType.NoRejection:
                    break;

                case RejectionType.MinMaxClipping:
                    trimmedMzValues = MinMaxClipping(mzValues);
                    break;

                case RejectionType.PercentileClipping:
                    trimmedMzValues = PercentileClipping(mzValues, options.Percentile);
                    break;

                case RejectionType.SigmaClipping:
                    trimmedMzValues = SigmaClipping(mzValues, options.MinSigmaValue, options.MaxSigmaValue);
                    break;

                case RejectionType.WinsorizedSigmaClipping:
                    trimmedMzValues = WinsorizedSigmaClipping(mzValues, options.MinSigmaValue, options.MaxSigmaValue);
                    break;

                case RejectionType.AveragedSigmaClipping:
                    trimmedMzValues = AveragedSigmaClipping(mzValues, options.MinSigmaValue, options.MaxSigmaValue);
                    break;

                case RejectionType.BelowThresholdRejection:
                    trimmedMzValues = BelowThresholdRejection(mzValues);
                    break;
            }
            return trimmedMzValues;
        }

        /// <summary>
        /// Reject the max and min of the set
        /// </summary>
        /// <param name="initialValues">array of mz values to evaluate</param>
        /// <returns>list of mz values with outliers rejected</returns>
        public static double[] MinMaxClipping(double[] initialValues)
        {
            double max = initialValues.Max();
            double min = initialValues.Min();

            return initialValues.Where(p => p < max && p > min).ToArray();
        }

        /// <summary>
        /// Removes values that fall outside of the central value by the defined percentile exclusively
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="percentile"></param>
        /// <returns>list of mz values with outliers rejected</returns>
        public static double[] PercentileClipping(double[] initialValues, double percentile)
        {
            double trim = (1 - percentile) / 2;
            double highPercentile = 1 - trim;
            double median = BasicStatistics.CalculateMedian(initialValues);
            double highCutoff = median * (1 + highPercentile);
            double lowCutoff = median * (1 - highPercentile); 
            // round to 4-6 decimal places
            return initialValues.Where(p => highCutoff > p && p > lowCutoff).ToArray();
        }

        /// <summary>
        /// Itteratively removes values that fall outside of the central value by the defined StandardDeviation amount
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        public static double[] SigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            List<double> values = initialValues.ToList();
            int n = 0;
            do
            {
                double median = BasicStatistics.CalculateMedian(values);
                double standardDeviation = BasicStatistics.CalculateStandardDeviation(values);
                n = 0;
                for (int i = 0; i < values.Count; i++)
                {
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {
                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0);
            double[] val = values.ToArray();
            return val;
        }

        /// <summary>
        /// Itteratively replaces values that fall outside of the central value by the defined StandardDeviation amount with the values of the median * that standard deviation amount
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        public static double[] WinsorizedSigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            List<double> values = initialValues.ToList();
            int n = 0;
            double iterationLimitforHuberLoop = 0.00005;
            double medianLeftBound;
            double medianRightBound;
            double windsorizedStandardDeviation;           
            do
            {
                if (!values.Any())
                    break;
                double median = BasicStatistics.CalculateNonZeroMedian(values);
                double standardDeviation = BasicStatistics.CalculateNonZeroStandardDeviation(values);
                double[] toProcess = values.ToArray();
                do // calculates a new median and standard deviation based on the values to do sigma clipping with (Huber loop)
                {
                    medianLeftBound = median - 1.5 * standardDeviation;
                    medianRightBound = median + 1.5 * standardDeviation;
                    toProcess.Winsorize(medianLeftBound, medianRightBound);
                    median = BasicStatistics.CalculateMedian(toProcess);
                    windsorizedStandardDeviation = standardDeviation;
                    standardDeviation = BasicStatistics.CalculateStandardDeviation(toProcess) * 1.134;
                } while (Math.Abs(standardDeviation - windsorizedStandardDeviation) / windsorizedStandardDeviation > iterationLimitforHuberLoop);

                n = 0;
                for (int i = 0; i < values.Count; i++)
                {
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {

                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0 && values.Count > 1); // break loop if nothing was rejected, or only one value remains
            return values.ToArray();
        }

        /// <summary>
        /// Iteratively removes values that fall outside of a calculated deviation based upon the median of the values
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        public static double[] AveragedSigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            List<double> values = initialValues.ToList();
            double median = BasicStatistics.CalculateNonZeroMedian(initialValues);
            double deviation = BasicStatistics.CalculateNonZeroStandardDeviation(initialValues, median);
            int n = 0;
            double standardDeviation;
            do
            {
                median = BasicStatistics.CalculateNonZeroMedian(values);
                standardDeviation = deviation * Math.Sqrt(median) / 10;

                n = 0;
                for (int i = 0; i < values.Count; i++)
                {
                    double temp = (values[i] - median) / standardDeviation;
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {

                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0);
            return values.ToArray();
        }

        /// <summary>
        /// Sets the array of mz values to null if they have 20% or fewer values than the number of scans
        /// </summary>
        /// <param name="initialValues">array of mz values to evaluate</param>
        /// <param name="cutoffValue">percent in decimal form of where to cutoff </param>
        /// <returns></returns>
        public static double[] BelowThresholdRejection(double[] initialValues, double cutoffValue = 0.2)
        {
            int scanCount = initialValues.Length;
            if (initialValues.Count(p => p != 0) <= scanCount * cutoffValue)
            {
                initialValues = new double[scanCount];
            }
            return initialValues;
        }

        #region Private Helpers

        /// <summary>
        /// Helper delegate method for sigma clipping
        /// </summary>
        /// <param name="value">the value in question</param>
        /// <param name="median">median of the dataset</param>
        /// <param name="standardDeviation">standard dev of the dataset</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        private static bool SigmaClipping(double value, double median, double standardDeviation, double sValueMin, double sValueMax)
        {
            double medianMinusvValue = (median - value) / standardDeviation; 
            double valueMinusMedian = (value - median) / standardDeviation;

            if (medianMinusvValue > sValueMin || valueMinusMedian > sValueMax)
            {
                return true; 
            }

            return false; 
        }

        /// <summary>
        /// Helper method to mutate the array of doubles based upon the median value
        /// </summary>
        /// <param name="initialValues">initial values to process</param>
        /// <param name="medianLeftBound">minimum the element in the dataset is allowed to be</param>
        /// <param name="medianRightBound">maxamum the element in the dataset is allowed to be</param>
        /// <returns></returns>
        private static void Winsorize(this double[] initialValues, double medianLeftBound, double medianRightBound)
        {
            for (int i = 0; i < initialValues.Length; i++)
            {
                if (initialValues[i] < medianLeftBound)
                {
                    if (i < initialValues.Length && initialValues.Any(p => p > medianLeftBound))
                        initialValues[i] = initialValues.First(p => p > medianLeftBound);
                    else
                        initialValues[i] = medianLeftBound;
                }
                else if (initialValues[i] > medianRightBound)
                {
                    if (i != 0 && initialValues.Any(p => p < medianRightBound))
                        initialValues[i] = initialValues.Last(p => p < medianRightBound);
                    else
                        initialValues[i] = medianRightBound;
                }
            }
        }

        #endregion

    }
}
