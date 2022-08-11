using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using TaskInterfaces; 

namespace Averaging
{
    public interface ISpectrumAveragingOptions : ITaskOptions
    {
        [Option('r', "rejection")]
        public RejectionType RejectionType { get; set; }
        [Option('w', "weighting")]
        public WeightingType WeightingType { get; set; }
        [Option('m', "merging")]
        public SpectrumMergingType SpectrumMergingType { get; set; } 
        [Option('p', "percentile")]
        public double Percentile { get; set; } 
        [Option("minsigma")]
        public double MinSigmaValue { get; set; } 
        [Option("maxsigma")]
        public double MaxSigmaValue { get; set; } 
        [Option('b', "binsize")]
        public double BinSize { get; set; } 
    }
    public class SpectrumAveragingOptions : ISpectrumAveragingOptions
    {
        public RejectionType RejectionType { get; set; }
        public WeightingType WeightingType { get; set; }
        public SpectrumMergingType SpectrumMergingType { get; set; }
        public double Percentile { get; set; }
        public double MinSigmaValue { get; set; }
        public double MaxSigmaValue { get; set; }
        public double BinSize { get; set; }
        public SpectrumAveragingOptions()
        {

        }

        /// <summary>
        /// Can be used to set the values of the options class in one method call
        /// </summary>
        /// <param name="rejectionType">rejection type to be used</param>
        /// <param name="percentile">percentile for percentile clipping rejection type</param>
        /// <param name="sigma">sigma value for sigma clipping rejection types</param>
        public void SetValues(RejectionType rejectionType = RejectionType.NoRejection,
            WeightingType intensityWeighingType = WeightingType.NoWeight, SpectrumMergingType spectrumMergingType = SpectrumMergingType.SpectrumBinning,
            double percentile = 0.1, double minSigma = 1.5, double maxSigma = 1.5, double binSize = 0.01)
        {
            RejectionType = rejectionType;
            WeightingType = intensityWeighingType;
            SpectrumMergingType = spectrumMergingType;
            Percentile = percentile;
            MinSigmaValue = minSigma;
            MaxSigmaValue = maxSigma;
            BinSize = binSize;
        }

        /// <summary>
        /// Sets the values of the options to their defaults
        /// </summary>
        public void SetDefaultValues()
        {
            RejectionType = RejectionType.NoRejection;
            WeightingType = WeightingType.NoWeight;
            SpectrumMergingType = SpectrumMergingType.SpectrumBinning;
            Percentile = 0.1;
            MinSigmaValue = 1.5;
            MaxSigmaValue = 1.5;
            BinSize = 0.01;
        }
    }
}
