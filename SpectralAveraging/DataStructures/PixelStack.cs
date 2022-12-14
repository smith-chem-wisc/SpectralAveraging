using System.ComponentModel;
using System.Xml.Serialization;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging;

public class PixelStack
{
    // binned spectrum -> pixel stack -> average combination
    public double Mz { get; set; }
    public List<double> Intensity { get; private set; }
    public int Length => Intensity.Count;
    public int NonRejectedLength => _pixels.Count(i => i.Rejected == false); 
    public double MergedValue { get; private set; }
    private List<Pixel> _pixels = new List<Pixel>();
    public IEnumerable<double> UnrejectedValues => _pixels
        .Where(i => i.Rejected == false)
        .Select(i => i.Intensity);
    
    public PixelStack(double mzVal)
    {
        Mz = mzVal;
        Intensity = new List<double>();
    }

    public PixelStack(IEnumerable<double> xArray, IEnumerable<double> yArray)
    {
        Mz = xArray.Where(i => !double.IsNaN(i)).Average();
        Intensity = yArray.ToList();
        _pixels = Intensity
            .Select((w, i) => new Pixel(i, w, false))
            .ToList(); 
    }

    public void Reject(int index)
    {
        _pixels[index].Rejected = true; 
    }

    public bool IsIndexRejected(int index)
    {
        return _pixels[index].Rejected;
    }

    public void ModifyPixelValues(int index, double value)
    {
        _pixels[index].Intensity = value; 
    }
    public void PerformRejection(SpectralAveragingOptions options)
    {
        OutlierRejection.RejectOutliers(this, options);
    }

    public void Average(IDictionary<int, double> weightsDictionary)
    {
        double numerator = 0;
        double denominator = 0;

        _pixels.Sort(new Pixel.PixelComparer());

        foreach (var weight in weightsDictionary)
        {
            int index = _pixels.BinarySearch(new Pixel() { SpectraId = weight.Key });
            if (index < 0) continue;
            if (_pixels[index].Rejected == true) continue;

            numerator += weight.Value * _pixels[index].Intensity;
            denominator += weight.Value; 
        }
        MergedValue = numerator / denominator; 
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

public class Pixel
{
    public int SpectraId;
    public double Intensity;
    public bool Rejected; 
    public Pixel(int spectraId, double intensity, bool rejected)
    {
        SpectraId = spectraId; 
        Intensity = intensity;
        Rejected = rejected;
    }

    public Pixel()
    {

    }

    internal class PixelComparer : IComparer<Pixel>
    {
        public int Compare(Pixel? x, Pixel? y)
        {
            return x.SpectraId.CompareTo(y.SpectraId); 
        }
    }
}