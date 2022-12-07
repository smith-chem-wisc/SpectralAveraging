using System.Diagnostics.CodeAnalysis;

namespace SpectralAveraging;

public static partial class OutlierRejection
{

    public static void RejectOutliers(PixelStack pixelStack, SpectralAveragingOptions options)
    {
        switch (options.RejectionType)
        {
            case RejectionType.NoRejection:
                break;

            case RejectionType.MinMaxClipping:
                MinMaxClipping(pixelStack);
                break;

            case RejectionType.PercentileClipping:
                PercentileClipping(pixelStack, options.Percentile);
                break;

            case RejectionType.SigmaClipping:
                SigmaClipping(pixelStack, options.MinSigmaValue, options.MaxSigmaValue);
                break;

            case RejectionType.WinsorizedSigmaClipping:
                WinsorizedSigmaClipping(pixelStack, options.MinSigmaValue, options.MaxSigmaValue);
                break;

            case RejectionType.AveragedSigmaClipping:
                AveragedSigmaClipping(pixelStack, options.MinSigmaValue, options.MaxSigmaValue);
                break;

            case RejectionType.BelowThresholdRejection:
                BelowThresholdRejection(pixelStack);
                break;
        }
    }
    /// <summary>
    /// Reject the max and min of the set
    /// </summary>
    /// <param name="initialValues">array of mz values to evaluate</param>
    /// <returns>list of mz values with outliers rejected</returns>
    public static void MinMaxClipping(PixelStack stack)
    {
        int max = stack.Intensity.IndexOf(stack.Intensity.Max());
        int min = stack.Intensity.IndexOf(stack.Intensity.Min());
        stack.Intensity[max] = double.NaN;
        stack.Intensity[min] = double.NaN; 
    }

    /// <summary>
    /// Removes values that fall outside of the central value by the defined percentile exclusively
    /// </summary>
    /// <param name="initialValues">list of mz values to evaluate</param>
    /// <param name="percentile"></param>
    /// <returns>list of mz values with outliers rejected</returns>
    public static void PercentileClipping(PixelStack pixelStack, double percentile)
    {
        double trim = (1 - percentile) / 2;
        double highPercentile = 1 - trim;
        double median = BasicStatistics.CalculateMedian(pixelStack.Intensity);
        double highCutoff = median * (1 + highPercentile);
        double lowCutoff = median * (1 - highPercentile);
        // round to 4-6 decimal places
        for (int i = 0; i < pixelStack.Length; i++)
        {
            if (pixelStack.Intensity[i] < highCutoff && pixelStack.Intensity[i] > lowCutoff)
            {
                continue; 
            }
            pixelStack.Intensity[i] = double.NaN; 
        }
    }

    /// <summary>
    /// Itteratively removes values that fall outside of the central value by the defined StandardDeviation amount
    /// </summary>
    /// <param name="initialValues">list of mz values to evaluate</param>
    /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
    /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
    /// <returns></returns>
    public static void SigmaClipping(PixelStack pixelStack, double sValueMin, double sValueMax)
    {
        int n = 0;
        do
        {
            double median = BasicStatistics.CalculateMedian(pixelStack.GetNonNaNValues());
            double standardDeviation = BasicStatistics.CalculateStandardDeviation(pixelStack.GetNonNaNValues());
            n = 0;
            for (int i = 0; i < pixelStack.Intensity.Count; i++)
            {
                if (double.IsNaN(pixelStack.Intensity[i])) continue; 
                if (!SigmaClipping(pixelStack.Intensity[i], median, standardDeviation, sValueMin, sValueMax)) continue;
                pixelStack.Intensity[i] = double.NaN;
                n++;
                i--;
            }
        } while (n > 0);
    }

    /// <summary>
    /// Itteratively replaces values that fall outside of the central value by the defined StandardDeviation amount with the values of the median * that standard deviation amount
    /// </summary>
    /// <param name="initialValues">list of mz values to evaluate</param>
    /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
    /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
    /// <returns></returns>
    public static void WinsorizedSigmaClipping(PixelStack pixelStack, double sValueMin, double sValueMax)
    {
        int n = 0;
        double iterationLimitforHuberLoop = 0.00005;
        double medianLeftBound;
        double medianRightBound;
        double windsorizedStandardDeviation;
        do
        {
            if (!pixelStack.Intensity.Any())
                break;
            double median = BasicStatistics.CalculateNonZeroMedian(pixelStack.GetNonNaNValues());
            double standardDeviation = BasicStatistics.CalculateNonZeroStandardDeviation(pixelStack.GetNonNaNValues());
            double[] toProcess = pixelStack.Intensity.ToArray();
            do // calculates a new median and standard deviation based on the values to do sigma clipping with (Huber loop)
            {
                medianLeftBound = median - 1.5 * standardDeviation;
                medianRightBound = median + 1.5 * standardDeviation;
                Winsorize(pixelStack, medianLeftBound, medianRightBound);
                median = BasicStatistics.CalculateMedian(toProcess);
                windsorizedStandardDeviation = standardDeviation;
                standardDeviation = BasicStatistics.CalculateStandardDeviation(toProcess) * 1.134;
            } while (Math.Abs(standardDeviation - windsorizedStandardDeviation) / windsorizedStandardDeviation > iterationLimitforHuberLoop);

            n = 0;
            for (int i = 0; i < pixelStack.Intensity.Count; i++)
            {
                if (SigmaClipping(pixelStack.Intensity[i], median, standardDeviation, sValueMin, sValueMax))
                {

                    pixelStack.Intensity[i] = double.NaN; 
                    n++;
                    i--;
                }
            }
        } while (n > 0 && pixelStack.Intensity.Count > 1); // break loop if nothing was rejected, or only one value remains
    }
    private static void Winsorize(PixelStack pixelStack, double medianLeftBound, double medianRightBound)
    {
        for (int i = 0; i < pixelStack.Length; i++)
        {
            if (double.IsNaN(pixelStack.Intensity[i])) continue; 
            if (pixelStack.Intensity[i] < medianLeftBound)
            {
                if (i < pixelStack.Length
                    && pixelStack.Intensity.Any(p => p > medianLeftBound))
                    pixelStack.Intensity[i] = pixelStack.Intensity.First(p => p > medianLeftBound);
                else
                    pixelStack.Intensity[i] = medianLeftBound;
            }
            else if (pixelStack.Intensity[i] > medianRightBound)
            {
                if (i != 0 && pixelStack.Intensity.Any(p => p < medianRightBound))
                    pixelStack.Intensity[i] = pixelStack.Intensity.Last(p => p < medianRightBound);
                else
                    pixelStack.Intensity[i] = medianRightBound;
            }
        }
    }

    /// <summary>
    /// Iteratively removes values that fall outside of a calculated deviation based upon the median of the values
    /// </summary>
    /// <param name="initialValues">list of mz values to evaluate</param>
    /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
    /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
    /// <returns></returns>
    public static void  AveragedSigmaClipping(PixelStack pixelStack, double sValueMin, double sValueMax)
    {
        double median = BasicStatistics.CalculateNonZeroMedian(pixelStack.GetNonNaNValues());
        double deviation = BasicStatistics.CalculateNonZeroStandardDeviation(pixelStack.GetNonNaNValues(), median);
        int n = 0;
        double standardDeviation;
        do
        {
            median = BasicStatistics.CalculateNonZeroMedian(pixelStack.Intensity);
            standardDeviation = deviation * Math.Sqrt(median) / 10;

            n = 0;
            for (int i = 0; i < pixelStack.Intensity.Count; i++)
            {
                double temp = (pixelStack.Intensity[i] - median) / standardDeviation;
                if (SigmaClipping(pixelStack.Intensity[i], median, standardDeviation, sValueMin, sValueMax))
                {

                    pixelStack.Intensity[i] = double.NaN;
                    n++;
                    i--;
                }
            }
        } while (n > 0);
    }

    /// <summary>
    /// Sets the array of mz values to null if they have 20% or fewer values than the number of scans
    /// </summary>
    /// <param name="initialValues">array of mz values to evaluate</param>
    /// <param name="cutoffValue">percent in decimal form of where to cutoff </param>
    /// <returns></returns>
    public static void BelowThresholdRejection(PixelStack pixelStack, double cutoffValue = 0.2)
    {
        var cutoffVal = pixelStack.Intensity.Max() * cutoffValue;
        for (int i = 0; i < pixelStack.Length; i++)
        {
            if (pixelStack.Intensity[i] < cutoffValue)
            {
                pixelStack.Intensity[i] = double.NaN; 
            }
        }
    }
}