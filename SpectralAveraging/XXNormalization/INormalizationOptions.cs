namespace SpectralAveraging
{
    public interface INormalizationOptions
    {
        public bool PerformNormalization { get; set; }
    }

    public class NormalizationOptions : INormalizationOptions
    {
        public bool PerformNormalization { get; set; }  
        public NormalizationOptions()
        {

        }
    }
}
