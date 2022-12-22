using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging; 
internal class Pixel
{
    public int SpectraId;
    public double Intensity;
    public double Mz;
    public bool Rejected;
    public Pixel(int spectraId, double mz, double intensity, bool rejected)
    {
        SpectraId = spectraId;
        Intensity = intensity;
        Rejected = rejected;
        Mz = mz;
    }

    internal class PixelComparer : IComparer<Pixel>
    {
        public int Compare(Pixel? x, Pixel? y)
        {
            return x.SpectraId.CompareTo(y.SpectraId);
        }
    }
}