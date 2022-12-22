using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging;

public class PixelStack
{
    // binned spectrum -> pixel stack -> average combination
    // TODO: Mz should not be a single value unless you're printing to console or getting the result. 
    // TODO: Store only values in _pixels and create GetIntensities method so you're only storing in 
    // one place. 
    public int Length => _pixels.Count;
    public int NonRejectedLength => _pixels.Count(i => i.Rejected == false); 
    public double MergedIntensityValue { get; private set; }
    public double MergedMzValue => CalculateMzAverage(); 
    public List<double> Intensity => GetIntensities().ToList();
    public List<double> Mzs => GetMzValues().ToList(); 
    public IEnumerable<double> UnrejectedIntensities => GetUnrejectedIntValues();
    public IEnumerable<double> UnrejectedMzs => GetUnrejectedMzValues();  
    private List<Pixel> _pixels { get; set; }
    
    public PixelStack(IEnumerable<double> xArray, IEnumerable<double> yArray)
    {
        _pixels = xArray.Zip(yArray, (mz, its) => (mz,its))
            .Select((m,n) => new Pixel(n, m.mz, m.its, rejected:false))
            .ToList();
        _pixels.Sort(new Pixel.PixelComparer());
    }

    private IEnumerable<double> GetIntensities()
    {
        return _pixels.Select(i => i.Intensity);
    }

    private IEnumerable<double> GetMzValues()
    {
        return _pixels.Select(i => i.Mz); 
    }
    private IEnumerable<double> GetUnrejectedIntValues()
    {
        return _pixels.Where(i => i.Rejected == false)
            .Select(i => i.Intensity); 
    }
    private IEnumerable<double> GetUnrejectedMzValues()
    {
        return _pixels.Where(i => i.Rejected == false)
            .Select(i => i.Mz);
    }

    public void Reject(int index)
    {
        _pixels[index].Rejected = true; 
    }

    public bool IsIndexRejected(int index)
    {
        return _pixels[index].Rejected;
    }

    internal void ModifyPixelIntensity(int index, double value)
    {
        _pixels[index].Intensity = value; 
    }

    internal void ModifyPixelMz(int index, double value)
    {
        _pixels[index].Intensity = value; 
    }

    public double GetIntensityAtIndex(int index)
    {
        return _pixels[index].Intensity; 
    }

    public double GetMzAtIndex(int index)
    {
        return _pixels[index].Mz; 
    }

    private double CalculateMzAverage()
    {
        return _pixels.Where(j => j.Rejected == false)
            .Average(j => j.Mz); 
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
            int index = _pixels.IndexOf(_pixels.Where(i => i.SpectraId == weight.Key).First()); 
            if (_pixels[index].Rejected == true) continue;

            numerator += weight.Value * _pixels[index].Intensity;
            denominator += weight.Value; 
        }
        MergedIntensityValue = numerator / denominator;
    }

    internal class PixelStackComparer: IComparer<PixelStack>
    {
        public int Compare(PixelStack x, PixelStack y)
        {
            return x.CalculateMzAverage().CompareTo(y.CalculateMzAverage());
        }
    }
}
