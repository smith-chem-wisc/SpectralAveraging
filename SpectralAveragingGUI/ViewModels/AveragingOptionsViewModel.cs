using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpectralAveraging;

namespace SpectralAveragingGUI
{
    public class AveragingOptionsViewModel : BaseViewModel
    {
        #region Private Members

        private SpectralAveragingOptions spectralAveragingOptions;

        #endregion

        #region Public Properties

        public SpectralAveragingOptions SpectralAveragingOptions
        {
            get { return spectralAveragingOptions; }
            set { spectralAveragingOptions = value; OnPropertyChanged(nameof(SpectralAveragingOptions)); }   
        }
        public RejectionType RejectionType
        {
            get { return spectralAveragingOptions.RejectionType; }
            set { spectralAveragingOptions.RejectionType = value; OnPropertyChanged(nameof(RejectionType));}
        }

        public WeightingType WeightingType
        {
            get { return spectralAveragingOptions.WeightingType; }
            set { spectralAveragingOptions.WeightingType = value; OnPropertyChanged(nameof(WeightingType)); }
        }

        public bool PerformNormalization
        {
            get { return spectralAveragingOptions.PerformNormalization; }
            set { spectralAveragingOptions.PerformNormalization = value; OnPropertyChanged(nameof(PerformNormalization)); }
        }

        public double Percentile
        {
            get { return spectralAveragingOptions.Percentile; }
            set { spectralAveragingOptions.Percentile = value; OnPropertyChanged(nameof(Percentile)); }
        }

        public double MinSigmaVale
        {
            get { return spectralAveragingOptions.MinSigmaValue; }
            set { spectralAveragingOptions.MinSigmaValue = value; OnPropertyChanged(nameof(MinSigmaVale)); }
        }

        public double MaxSigmaValue
        {
            get { return spectralAveragingOptions.MaxSigmaValue; }
            set { spectralAveragingOptions.MaxSigmaValue = value; OnPropertyChanged(nameof(MaxSigmaValue)); }
        }

        public double BinSize
        {
            get { return spectralAveragingOptions.BinSize; }
            set { spectralAveragingOptions.BinSize = value; OnPropertyChanged(nameof(BinSize)); }
        }

        public RejectionType[] RejectionTypes { get; set; }
        public WeightingType[] WeightingTypes { get; set; }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public AveragingOptionsViewModel(SpectralAveragingOptions options)
        {
            // value initialization
            spectralAveragingOptions = options;
            RejectionTypes = ((RejectionType[])Enum.GetValues(typeof(RejectionType))).Where(p => p != RejectionType.Thermo).ToArray();
            WeightingTypes = ((WeightingType[])Enum.GetValues(typeof(WeightingType)));

            // command assignment
        }

        #endregion

        #region Helpers

        public void ResetDefaults()
        {
            SpectralAveragingOptions.SetDefaultValues();
            OnPropertyChanged(nameof(SpectralAveragingOptions));
            OnPropertyChanged(nameof(RejectionType));
            OnPropertyChanged(nameof(WeightingType));
            OnPropertyChanged(nameof(PerformNormalization));
            OnPropertyChanged(nameof(Percentile));
            OnPropertyChanged(nameof(MinSigmaVale));
            OnPropertyChanged(nameof(MaxSigmaValue));
            OnPropertyChanged(nameof(BinSize));
        }

        #endregion

    }
}
