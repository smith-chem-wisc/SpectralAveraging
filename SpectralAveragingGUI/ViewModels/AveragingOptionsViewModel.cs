using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Nett;
using SpectralAveraging;
using ThermoFisher.CommonCore.Data.Business;

namespace SpectralAveragingGUI
{
    public class AveragingOptionsViewModel : BaseViewModel
    {
        #region Private Members

        private SpectralAveragingOptions spectralAveragingOptions;
        private int previousOverlap;
        private string? savedPath;

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

        public SpectraFileProcessingType SpectraFileProcessingType
        {
            get { return spectralAveragingOptions.SpectraFileProcessingType; }
            set { spectralAveragingOptions.SpectraFileProcessingType = value;
                if ((SpectraFileProcessingType)value is SpectraFileProcessingType.AverageDDAScans
                    or SpectraFileProcessingType.AverageEverynScans)
                {
                    if (previousOverlap == 0)
                    {
                        previousOverlap = ScanOverlap;
                    }

                    ScanOverlap = 0;
                }
                else if (previousOverlap != 0)
                {
                    ScanOverlap = previousOverlap;
                }
                
                OnPropertyChanged(nameof(SpectraFileProcessingType));
            }
        }

        public int NumberOfScansToAverage
        {
            get { return spectralAveragingOptions.NumberOfScansToAverage; }
            set { spectralAveragingOptions.NumberOfScansToAverage = value; OnPropertyChanged(nameof(NumberOfScansToAverage)); }
        }

        public int ScanOverlap
        {
            get { return spectralAveragingOptions.ScanOverlap; }
            set
            {
                if (value >= NumberOfScansToAverage)
                    MessageBox.Show("Overlap cannot be greater than or equal to the number of spectra averaged");
                else
                {
                    spectralAveragingOptions.ScanOverlap = value;
                    OnPropertyChanged(nameof(ScanOverlap));
                }
            }
        }

        public string Name
        {
            get
            {
                if (savedPath != null)
                    return Path.GetFileNameWithoutExtension(savedPath);
                else
                    return "Default Options";
            }
        }

        public string SavedPath
        {
            get => savedPath;
            set { savedPath = value; OnPropertyChanged(nameof(SavedPath)); }
        }

        public OutputType OutputType
        {
            get { return spectralAveragingOptions.OutputType; }
            set { spectralAveragingOptions.OutputType = value; OnPropertyChanged(nameof(OutputType)); }
        }

        public RejectionType[] RejectionTypes { get; set; }
        public WeightingType[] WeightingTypes { get; set; }
        public SpectraFileProcessingType[] SpectraFileProcessingTypes { get; set; }
        public OutputType[] OutputTypes { get; set; }

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
            SpectraFileProcessingTypes = ((SpectraFileProcessingType[])Enum.GetValues(typeof(SpectraFileProcessingType)));
            OutputTypes = ((OutputType[])Enum.GetValues(typeof(OutputType)));

            // command assignment
        }

        #endregion

        #region Helpers

        public void ResetDefaults()
        {
            SpectralAveragingOptions.SetDefaultValues();
            UpdateVisualRepresentation();
        }

        public void UpdateVisualRepresentation()
        {
            OnPropertyChanged(nameof(SpectralAveragingOptions));
            OnPropertyChanged(nameof(RejectionType));
            OnPropertyChanged(nameof(WeightingType));
            OnPropertyChanged(nameof(PerformNormalization));
            OnPropertyChanged(nameof(Percentile));
            OnPropertyChanged(nameof(MinSigmaVale));
            OnPropertyChanged(nameof(MaxSigmaValue));
            OnPropertyChanged(nameof(BinSize));
            OnPropertyChanged(nameof(SpectraFileProcessingType));
            OnPropertyChanged((nameof(NumberOfScansToAverage)));
            OnPropertyChanged(nameof(ScanOverlap));
            OnPropertyChanged(nameof(OutputType));
            OnPropertyChanged(nameof(Name));
        }

        public void SaveOptions()
        {
            Toml.WriteFile(spectralAveragingOptions, savedPath);
        }

        public void DeleteOptions()
        {
            File.Delete(savedPath);
        }

        #endregion

    }
}
