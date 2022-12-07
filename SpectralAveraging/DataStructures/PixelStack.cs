using System.ComponentModel;
using System.Xml.Serialization;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging;

public class PixelStack
{
    // binned spectrum -> pixel stack -> average combination
    public double Mz { get; }
    public List<double> Intensity { get; private set; }
    public double MergedValue { get; private set; }

    public PixelStack(double mzVal)
    {
        Mz = mzVal;
        Intensity = new List<double>();
    }

    public void AddIntensityVals(IEnumerable<double> yVals)
    {
        Intensity.AddRange(yVals);
    }

    public void PerformRejection(SpectralAveragingOptions options)
    {
        double[] results = OutlierRejection.RejectOutliers(Intensity.ToArray(), options);
        // need to maintain the length in and the length out. Use placeholder value if 
        // value is rejected and not replaced. This will affect certain methods like the 
        // belowThresholdRejection, Percentile Clipping, and min/max clipping
    }

    public void PerformMerging(double[] weights)
    {

    }
}

public class BinnedSpectra
{
    public List<PixelStack> PixelStacks { get; set; }
    public Dictionary<int, double> Weights { get; set; }

    public BinnedSpectra()
    {
        PixelStacks = new List<PixelStack>();
        Weights = new Dictionary<int, double>();
    }
    public double[] ProcessPixelStacks(SpectralAveragingOptions options)
    {
        foreach (var pixelStack in PixelStacks)
        {
            pixelStack.PerformRejection(options);
        }
    }

    public void ConsumeSpectra(double[][] xArrays, double[][] yArrays, double[] totalIonCurrents, int numSpectra, 
        double binSize)
    {
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

        double[] xArray = new double[xValuesBin.Length];


        for (var i = 0; i < xArray.Length; i++)
        {
            xArray[i] = xValuesBin[i].Where(p => p != 0).Average();
            double x = xArray[i];
            var pixelStack = new PixelStack(x);
            pixelStack.AddIntensityVals(yValuesBin[i]);
            PixelStacks.Add(pixelStack);
        }
    }

    public void PerformNormalization(double[][] yArrays, double[] tics)
    {
        for (int i = 0; i < yArrays.Length; i++)
        {
            SpectrumNormalization.NormalizeSpectrumToTic(yArrays[i], 
                tics[i], tics.Average());
        }
    }
    public void CalculateWeights(double[][] yArrays)
    {
        for (var i = 0; i < yArrays.Length; i++)
        {
            double[] yArray = yArrays[i];
            bool success = MRSNoiseEstimator.MRSNoiseEstimation(yArray, 0.01, out double noiseEstimate);
            // if the MRS noise estimate fails to converge, go by regular standard deviation
            if (!success)
            {
                noiseEstimate = BasicStatistics.CalculateStandardDeviation(yArray);
            }
            Weights.Add(i, noiseEstimate);
        }
    }
    
}