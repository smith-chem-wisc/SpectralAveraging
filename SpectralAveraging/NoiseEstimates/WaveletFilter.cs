namespace SpectralAveraging.NoiseEstimates;

public class WaveletFilter
{
    public double[] WaveletCoefficients { get; private set; }
    public double[] ScalingCoefficients { get; private set; }
    public WaveletType WaveletType { get; private set; }

    public void CreateFiltersFromCoeffs(double[] filterCoeffs)
    {
        WaveletCoefficients = new double[filterCoeffs.Length];
        ScalingCoefficients = new double[filterCoeffs.Length];

        // calculate wavelet coefficients
        for (int i = 0; i < ScalingCoefficients.Length; i++)
        {
            ScalingCoefficients[i] = filterCoeffs[i] / Math.Sqrt(2d);
        }
        WaveletCoefficients = WaveletMathUtils.QMF(ScalingCoefficients, inverse: true);
    }

    public void CreateFiltersFromCoeffs(WaveletType waveletType)
    {
        switch (waveletType)
        {
            case WaveletType.Haar:
            {
                WaveletType = WaveletType.Haar;
                CreateFiltersFromCoeffs(_haarCoefficients);
                return;
            }
        }
    }
    private readonly double[] _haarCoefficients =
    {
        0.7071067811865475,
        0.7071067811865475
    };
}