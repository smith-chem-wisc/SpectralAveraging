using System.Text;

namespace SpectralAveraging.NoiseEstimates;

public class ModWtOutput
{
    public ModWtOutput(int maxScale, WaveletType waveletType, BoundaryType boundaryType)
    {
        Levels = new List<Level>();
        MaxScale = maxScale;
        WaveletType = waveletType;
        BoundaryType = boundaryType;
    }

    public List<Level> Levels { get; private set; }
    public int MaxScale { get; private set; }
    public WaveletType WaveletType { get; }
    public BoundaryType BoundaryType { get; }
    public void AddLevel(Level level)
    {
        Levels.Add(level);
    }

    public void AddLevel(double[] waveletCoeff, double[] scalingCoeff, int scale,
        BoundaryType boundaryType, int originalSignalLength, int filterLength)
    {
        if (boundaryType == BoundaryType.Reflection)
        {
            int startIndex = ((int)Math.Pow(2, scale)) * (filterLength - 1);
            int stopIndex = startIndex + originalSignalLength;
            Levels.Add(new Level(scale,
                waveletCoeff[startIndex..stopIndex],
                scalingCoeff[startIndex..stopIndex]));
        }
    }

    public void PrintToTxt(string path)
    {
        StringBuilder sb = new();
        foreach (var level in Levels)
        {
            sb.AppendLine(string.Join("\t", level.WaveletCoeff));
            sb.AppendLine(string.Join("\t", level.ScalingCoeff));
        }
        File.WriteAllText(path, sb.ToString());
    }
}