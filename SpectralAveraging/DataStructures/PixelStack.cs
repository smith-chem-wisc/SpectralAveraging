using System.ComponentModel;
using System.Xml.Serialization;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging;

public class PixelStack
{
    // binned spectrum -> pixel stack -> average combination
    public double Mz { get; set; }
    public List<double> Intensity { get; private set; }

    public int[] SpectraIDs { get; private set; }
    public int Length => Intensity.Count;
    public int NonNaNLength => Intensity.Count(i => !double.IsNaN(i)); 
    public double MergedValue { get; private set; }
    private List<int> RejectedIndices { get; set; }
    private List<double> ValuesAfterRejection { get; set; }


    public PixelStack(double mzVal)
    {
        Mz = mzVal;
        Intensity = new List<double>();
    }

    public PixelStack(IEnumerable<double> xArray, IEnumerable<double> yArray, int[] spectraId)
    {
        Mz = xArray.Where(i => !double.IsNaN(i)).Average();
        Intensity = yArray.ToList();
        SpectraIDs = spectraId;
        RejectedIndices = new(); 
        ValuesAfterRejection = new List<double>();
    }
    public IEnumerable<double> GetNonNaNValues()
    {
        return Intensity.Where(i => !double.IsNaN(i)); 
    }
    
    public void AddIntensityVals(IEnumerable<double> yVals)
    {
        Intensity.AddRange(yVals);
    }

    public void AddMzVals(IEnumerable<double> xVals)
    {
        Mz = xVals.Average(); 
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
    public void RejectValue(int spectraId)
    {
        RejectedValueIndices.Add(spectraId);
    }
    // People using the code should only ever see the list of doubles that represent 
    // the intensity values, and the merged spectra. 
    // But the actual backing probably needs to be more complicated than that. 
    // Use a sorted dictionary as the store

    internal class PixelStackComparer: IComparer<PixelStack>
    {
        public int Compare(PixelStack x, PixelStack y)
        {
            return x.Mz.CompareTo(y.Mz);
        }
    }
}