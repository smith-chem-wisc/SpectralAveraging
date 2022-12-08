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
            if (double.IsNaN(Intensity[i])) 
                continue;
            numerator += weights[i] * Intensity[i];
            denominator += weights[i];
        }
        MergedValue = numerator / denominator; 
    }
}