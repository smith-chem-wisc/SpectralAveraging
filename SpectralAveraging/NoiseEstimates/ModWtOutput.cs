using System.Text;

namespace SpectralAveraging.NoiseEstimates;

internal class ModWtOutput
{
    internal ModWtOutput(int maxScale, WaveletType waveletType, BoundaryType boundaryType)
    {
        Levels = new List<Level>();
        MaxScale = maxScale;
        WaveletType = waveletType;
        BoundaryType = boundaryType;
    }

    internal List<Level> Levels { get; private set; }
    internal int MaxScale { get; private set; }
    internal WaveletType WaveletType { get; }
    internal BoundaryType BoundaryType { get; }

    internal void AddLevel(double[] waveletCoeff, double[] scalingCoeff, int scale,
        BoundaryType boundaryType, int originalSignalLength, int filterLength)
    {
        if (boundaryType == BoundaryType.Reflection)
        {
            int startIndex = ((int)Math.Pow(2, scale) - 1) * (filterLength - 1);
            int stopIndex = Math.Min(startIndex + originalSignalLength, waveletCoeff.Length - 1);
            Levels.Add(new Level(scale,
                waveletCoeff[startIndex..stopIndex],
                scalingCoeff[startIndex..stopIndex]));
        }
    }
}