using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using MassSpectrometry;
using MzLibUtil;
using Data;

namespace Averaging
{
    public static class SpectrumAveraging
    {
        #region Public Entry Point Methods

        /// <summary>
        /// Calls the specific merging function based upon the current static field SpecrimMergingType
        /// </summary>
        /// <param name="scans"></param>
        public static MzSpectrum CombineSpectra(double[][] xArrays, double[][] yArrays, int numSpectra, SpectrumAveragingOptions options)
        {
            MzSpectrum compositeSpectrum = null;
            switch (options.SpectrumMergingType)
            {
                case SpectrumMergingType.SpectrumBinning:
                    compositeSpectrum = SpectrumBinning(xArrays, yArrays, options.BinSize, numSpectra, options);
                    break;


                case SpectrumMergingType.MostSimilarSpectrum:
                    MostSimilarSpectrum();
                    break;
            }
            return compositeSpectrum;
        }

        /// <summary>
        /// Override to use MultiScanDataObjects in SpectrumAverager
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static void CombineSpectra(MultiScanDataObject data, SpectrumAveragingOptions options)
        {
            MzSpectrum compositeSpectrum = CombineSpectra(data.XArrays, data.YArrays, data.ScansToProcess, options);
            data.CompositeSpectrum = compositeSpectrum;
        }

        /// <summary>
        /// Override to use MultiScanDataObjecs with default averaging settings
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void CombineSpectra(MultiScanDataObject data)
        {
            SpectrumAveragingOptions options = new();
            options.SetDefaultValues();
            MzSpectrum compositeSpectrum = CombineSpectra(data.XArrays, data.YArrays, data.ScansToProcess, options);
            data.CompositeSpectrum = compositeSpectrum;
        }

        #endregion

        #region Misc Processing Methods

        /// <summary>
        /// Main Engine of this class, processes a single array of intesnity values for a single mz and returns their average
        /// </summary>
        /// <param name="intInitialArray"></param>
        /// <returns></returns>
        public static double ProcessSingleMzArray(double[] intInitialArray, SpectrumAveragingOptions options)
        {
            double average;
            double[] weights;
            double[] trimmedArray;

            if (intInitialArray.Where(p => p != 0).Count() <= 1)
                return 0;
            else
            {
                trimmedArray = RejectOutliers(intInitialArray, options);
                if (trimmedArray.Where(p => p != 0).Count() <= 1)
                    return 0;
                //else if (trimmedArray.Where(p => p != 0).Count() == 1)
                //    average = trimmedArray.First(p => p != 0) / intInitialArray.Length;
                else
                {
                    weights = CalculateWeights(trimmedArray, options.WeightingType);
                    average = MergePeakValuesToAverage(trimmedArray, weights);
                }
            }
            return average;
        }

        /// <summary>
        /// Calculates the weighted average value for each m/z point passed to it
        /// </summary>
        /// <param name="intValues">array of mz values to evaluate </param>
        /// <param name="weights">relative weight assigned to each of the mz values</param>
        /// <returns></returns>
        public static double MergePeakValuesToAverage(double[] intValues, double[] weights)
        {
            double numerator = 0;
            for (int i = 0; i < intValues.Count(); i++)
            {
                numerator += intValues[i] * weights[i];
            }
            double average = numerator / weights.Sum();
            return average;
        }

        

        #endregion

        #region Rejection Functions

        /// <summary>
        /// Calls the specific rejection function based upon the current static field RejectionType
        /// </summary>
        /// <param name="mzValues">list of mz values to evaluate<</param>
        /// <returns></returns>
        public static double[] RejectOutliers(double[] mzValues, SpectrumAveragingOptions options)
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

            double[] clippedValues = initialValues.Where(p => p < max && p > min).ToArray();
            //CheckValuePassed(clippedValues);
            return clippedValues;
        }

        /// <summary>
        /// Removes values that fall outside of the central value by the defined percentile exclusively
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="percentile"></param>
        /// <returns>list of mz values with outliers rejected</returns>
        public static double[] PercentileClipping(double[] initialValues, double percentile)
        {
            double median = CalculateNonZeroMedian(initialValues);
            double[] clippedValues = initialValues.Where(p => (median - p) / median > -percentile && (median - p) / median < percentile).ToArray();
            return clippedValues;
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
                double median = CalculateMedian(values);
                double standardDeviation = CalculateStandardDeviation(values);
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
            double iterationLimitforHuberLoop = 0.01;
            double averageAbsoluteDeviation = Math.Sqrt(2 / Math.PI) * (sValueMax + sValueMin) / 2;
            double medianLeftBound;
            double medianRightBound;
            double windsorizedStandardDeviation;
            do
            {
                if (!values.Any())
                    break;
                double median = CalculateNonZeroMedian(values);
                double standardDeviation = CalculateNonZeroStandardDeviation(values);
                double[] toProcess = values.ToArray();
                do // calculates a new median and standard deviation based on the values to do sigma clipping with (Huber loop)
                {
                    medianLeftBound = median - sValueMin * standardDeviation;
                    medianRightBound = median + sValueMax * standardDeviation;
                    Winsorize(toProcess, medianLeftBound, medianRightBound);
                    median = CalculateMedian(toProcess);
                    windsorizedStandardDeviation = standardDeviation;
                    standardDeviation = averageAbsoluteDeviation > 1 ? CalculateStandardDeviation(toProcess) * averageAbsoluteDeviation : CalculateStandardDeviation(toProcess) * 1.05;
                    double value = Math.Abs(standardDeviation - windsorizedStandardDeviation) / windsorizedStandardDeviation;

                } while (Math.Abs(standardDeviation - windsorizedStandardDeviation) / windsorizedStandardDeviation > iterationLimitforHuberLoop);

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
            double[] val = values.ToArray();
            return val;
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
            double median = CalculateNonZeroMedian(initialValues);
            double deviation = CalculateNonZeroStandardDeviation(initialValues, median);
            int n = 0;
            double standardDeviation;
            do
            {
                median = CalculateNonZeroMedian(values);
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
            double[] val = values.ToArray();
            return val;
        }

        /// <summary>
        /// Sets the array of mz values to null if they have 20% or fewer values than the number of scans
        /// </summary>
        /// <param name="initialValues">array of mz values to evaluate</param>
        /// <param name="scanCount">number of scans used to create initialValues</param>
        /// <returns></returns>
        public static double[] BelowThresholdRejection(double[] initialValues, double cutoffValue = 0.2)
        {
            int scanCount = initialValues.Length;
            if (initialValues.Count() <= scanCount * cutoffValue)
            {
                initialValues = new double[scanCount];
            }
            else if (initialValues.Where(p => p != 0).Count() <= scanCount * cutoffValue)
            {
                initialValues = new double[scanCount];
            }
            return initialValues;
        }

        #endregion

        #region Weighing Functions

        /// <summary>
        /// Calls the specicic funtion based upon the settings to calcuate the weight for each value when averaging
        /// </summary>
        /// <param name="mzValues"></param>
        public static double[] CalculateWeights(double[] mzValues, WeightingType weightingType)
        {
            double[] weights = new double[mzValues.Length];

            switch (weightingType)
            {
                case WeightingType.NoWeight:
                    for (int i = 0; i < weights.Length; i++)
                        weights[i] = 1;
                    break;

                case WeightingType.NormalDistribution:
                    WeightByNormalDistribution(mzValues, ref weights);
                    break;

                case WeightingType.CauchyDistribution:
                    WeightByCauchyDistribution(mzValues, ref weights);
                    break;

                case WeightingType.PoissonDistribution:
                    WeightByPoissonDistribution(mzValues, ref weights);
                    break;

                case WeightingType.GammaDistribution:
                    WeightByGammaDistribution(mzValues, ref weights);
                    break;

            }


            return weights;
        }


        /// <summary>
        /// Weights the mzValues based upon a normal distribution
        /// </summary>
        /// <param name="mzValues">intensities for a single mz value</param>
        /// <param name="weights">calculated weights for each intensity</param>
        public static void WeightByNormalDistribution(double[] mzValues, ref double[] weights)
        {
            double standardDeviation = CalculateStandardDeviation(mzValues);
            double mean = mzValues/*.Where(p => p != 0)*/.Average();

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Normal.PDF(mean, standardDeviation, mzValues[i]);
            }
        }

        /// <summary>
        /// Weights the mzValues based upon a cauchy distribution
        /// </summary>
        /// <param name="mzValues">intensities for a single mz value</param>
        /// <param name="weights">calculated weights for each intensity</param>
        public static void WeightByCauchyDistribution(double[] mzValues, ref double[] weights)
        {
            double standardDeviation = CalculateStandardDeviation(mzValues);
            double mean = mzValues.Average();

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Cauchy.PDF(mean, standardDeviation, mzValues[i]);
            }
        }

        /// <summary>
        /// Weights the mzValues based upon a gamma distribution
        /// </summary>
        /// <param name="mzValues">intensities for a single mz value</param>
        /// <param name="weights">calculated weights for each intensity</param>
        public static void WeightByGammaDistribution(double[] mzValues, ref double[] weights)
        {
            double standardDeviation = CalculateStandardDeviation(mzValues);
            double mean = mzValues.Average();
            double rate = mean / Math.Pow(standardDeviation, 2);
            double shape = mean * rate;

            for (int i = 0; i < weights.Length; i++)
            {
                if (mzValues[i] < mean)
                    weights[i] = Gamma.CDF(shape, rate, mzValues[i]);
                else
                    weights[i] = 1- Gamma.CDF(shape, rate, mzValues[i]);
                //weights[i] = double.IsInfinity(Gamma.PDF(shape, rate, mzValues[i])) ? 0 : Gamma.PDF(shape, rate, mzValues[i]);
            }
        }

        /// <summary>
        /// Weights the mzValues based upon a poisson distribution
        /// </summary>
        /// <param name="mzValues">intensities for a single mz value</param>
        /// <param name="weights">calculated weights for each intensity</param>
        public static void WeightByPoissonDistribution(double[] mzValues, ref double[] weights)
        {
            double mean = mzValues.Average();

            for (int i = 0; i < weights.Length; i++)
            {
                if (mzValues[i] < mean)
                    weights[i] = 1 - Poisson.CDF(mean, mzValues[i]);
                else if (mzValues[i] > mean)
                    weights[i] = Poisson.CDF(mean, mzValues[i]);
            }
        }

        #endregion

        #region Merging Functions


        /// <summary>
        /// Merges spectra into a two dimensional array of (m/z, int) values based upon their bin 
        /// </summary>
        /// <param name="scans">scans to be combined</param>
        /// <returns>MSDataScan with merged values</returns>
        public static MzSpectrum SpectrumBinning(double[][] xArrays, double[][] yArrays, double binSize, int numSpectra, 
            SpectrumAveragingOptions options)
        {
            // calculate the bins to be utilizied
            double min = 100000;
            double max = 0;
            for (int i = 0; i < numSpectra; i++)
            {
                min = Math.Min(xArrays[i][0], min);
                max = Math.Max(xArrays[i].Max(), max);
            }
            int numberOfBins = (int)Math.Ceiling((max - min) * (1 / binSize));

            double[][] xValuesBin = new double[numberOfBins][];
            double[][] yValuesBin = new double[numberOfBins][];
            // go through each scan and place each (m/z, int) from the spectra into a jagged array
            for (int i = 0; i < numSpectra; i++)
            {
                for (int j = 0; j < xArrays[i].Length; j++)
                {
                    int binIndex = (int)Math.Floor((xArrays[i][j] - min) / binSize);
                    if (xValuesBin[binIndex] == null)
                    {
                        xValuesBin[binIndex] = new double[numSpectra];
                        yValuesBin[binIndex] = new double[numSpectra];
                    }
                    xValuesBin[binIndex][i] = xArrays[i][j];
                    yValuesBin[binIndex][i] = yArrays[i][j];
                }
            }

            xValuesBin = xValuesBin.Where(p => p != null).ToArray();
            yValuesBin = yValuesBin.Where(p => p != null).ToArray();

            // average the remaining arrays to create the composite spectrum
            // this will clipping and avereraging for y values as indicated in the settings
            double[] xArray = new double[xValuesBin.Length];
            double[] yArray = new double[yValuesBin.Length];
            for (int i = 0; i < yValuesBin.Length; i++)
            {
                xArray[i] = xValuesBin[i].Where(p => p != 0).Average();
                yArray[i] = ProcessSingleMzArray(yValuesBin[i], options);
            }

            // Create new MsDataScan to return
            MzRange range = new(min, max);
            MzSpectrum mergedSpectra = new(xArray, yArray, true);

            return mergedSpectra;
        }
        public static MzSpectrum MostSimilarSpectrum()
        {
            throw new NotImplementedException();
        }

        #endregion

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
            if ((median - value) / standardDeviation > sValueMin)
            {
                return true;
            }
            else if ((value - median) / standardDeviation > sValueMax)
            {
                return true;
            }
            else
            {
                return false;
            }
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
                    initialValues[i] = medianLeftBound;
                }
                else if (initialValues[i] > medianRightBound)
                {
                    initialValues[i] = medianRightBound;
                }
            }
        }

        /// <summary>
        /// Calculates the median of a list of doubles
        /// </summary>
        /// <param name="toCalc">initial list to calculate from</param>
        /// <returns>double representation of the median</returns>
        private static double CalculateMedian(IEnumerable<double> toCalc)
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

        private static double CalculateNonZeroMedian(IEnumerable<double> toCalc)
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
        public static double CalculateStandardDeviation(IEnumerable<double> toCalc, double average = 0)
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

        private static double CalculateStandardDeviation(double[] toCalc, double average = 0)
        {
            return CalculateStandardDeviation((IEnumerable<double>)toCalc, average);
        }

        private static double CalculateNonZeroStandardDeviation(IEnumerable<double> toCalc, double average = 0)
        {
            toCalc = toCalc.Where(p => p != 0);
            if (toCalc.Count() == 0)
                return 0;
            else
                return CalculateStandardDeviation(toCalc, average);
        }

        #endregion
    }
}