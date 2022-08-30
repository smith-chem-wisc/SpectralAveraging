using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MzLibUtil;
using UsefulProteomicsDatabases;
using SpectralAveraging;


namespace SpectralAveragingExtensions
{
    public class AveragedScanAnalyzer
    {
        public double Resolution { get; set; }
        public double NumberOfScans { get; set; }
        public SpectralAveragingOptions Options { get; set; }
        public MzSpectrum CompositeSpectrum { get; set; }
        public double SignalToNoiseScoringTolerance { get; set; }
        public double SignalToNoiseCutoffLevel { get; set; }

        #region Constructors

        // use this one!
        public AveragedScanAnalyzer(SpectralAveragingOptions options, MzSpectrum compositeSpectrum, MultiScanDataObject data, double resolution)
        {
            NumberOfScans = data.ScansToProcess;
            Resolution = resolution;
            Options = options;
            CompositeSpectrum = data.CompositeSpectrum;
        }

        #endregion

       


    }

}
