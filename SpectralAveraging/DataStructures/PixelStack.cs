using System.ComponentModel;
using System.Xml.Serialization;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging;

public class PixelStack
{
    // binned spectrum -> pixel stack -> average combination
    public double Mz { get; }
    public List<double> Intensity { get; private set; }
    public int Length => Intensity.Count;
    public int NonNaNLength => Intensity.Count(i => !double.IsNaN(i)); 
    public double MergedValue { get; private set; }

    public PixelStack(double mzVal)
    {
        Mz = mzVal;
        Intensity = new List<double>();
    }
    public IEnumerable<double> GetNonNaNValues()
    {
        return Intensity.Where(i => !double.IsNaN(i)); 
    }
    
    public void AddIntensityVals(IEnumerable<double> yVals)
    {
        Intensity.AddRange(yVals);
    }

    public void PerformRejection(SpectralAveragingOptions options)
    {
        OutlierRejection.RejectOutliers(this, options);
    }

    public void Average(double[] weights)
    {
        double numerator = 0;
        double denominator = 0; 

        for (int i = 0; i < Length; i++)
        {
            if (!double.IsNaN(Intensity[i]))
            { 
                numerator += weights[i] * Intensity[i];
                denominator += weights[i];
            }
        }
        MergedValue = numerator / denominator; 
    }
}

public class BinnedSpectra
{
    public List<PixelStack> PixelStacks { get; set; }
    public Dictionary<int, double> NoiseEstimates { get; private set; }
    public Dictionary<int, double> ScaleEstimates { get; private set; }
    public Dictionary<int, double> Weights { get; private set; }

    public BinnedSpectra()
    {
        PixelStacks = new List<PixelStack>();
        NoiseEstimates = new Dictionary<int, double>();
        ScaleEstimates = new Dictionary<int, double>();
        Weights = new Dictionary<int, double>(); 
    }
    public void ProcessPixelStacks(SpectralAveragingOptions options)
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
    public void CalculateNoiseEstimates(double[][] yArrays)
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
            NoiseEstimates.Add(i, noiseEstimate);
        }
    }

    public void CalculateScaleEstimates()
    {
        // insert avgdev method code here
    }

    public void CalculateWeights()
    {
        foreach (var entry in NoiseEstimates)
        {
            var successScale = ScaleEstimates.TryGetValue(entry.Key, 
                out double scale);
            if (!successScale) continue; 
            
            var successNoise = NoiseEstimates.TryGetValue(entry.Key,
                out double noise);
            if (!successNoise) continue;

            double weight = 1d / Math.Pow((scale * noise), 2);

            Weights.Add(entry.Key, weight); 
        } 
    }
    public void MergeSpectra()
    {
        double[] weights = Weights.OrderBy(i => i.Key)
            .Select(i => i.Value)
            .ToArray(); 
        foreach (var pixelStack in PixelStacks)
        {
            pixelStack.Average(weights); 
        }
    }
    public double[][] GetMergedSpectrum()
    {
        double[] xArray = PixelStacks.Select(i => i.Mz).ToArray();
        double[] yArray = PixelStacks.Select(i => i.MergedValue).ToArray();
        return new[] { xArray, yArray }; 
    }

}