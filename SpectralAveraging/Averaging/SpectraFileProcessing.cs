using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace SpectralAveraging
{
    /// <summary>
    /// Manages the spectra files being averaged
    /// </summary>
    public static class SpectraFileProcessing
    {
        public static void ProcessSpectra(List<MsDataScan> scans, SpectralAveragingOptions options)
        {
            switch (options.SpectraFileProcessingType)
            {
                case SpectraFileProcessingType.AverageAll:
                    AverageAll(scans, options);
                    break;

                case SpectraFileProcessingType.AverageEverynScans:
                    AverageEverynScans(scans, options);
                    break;

                case SpectraFileProcessingType.AverageEverynScansWithOverlap:
                    AverageEverynScans(scans, options);
                    break;

                case SpectraFileProcessingType.AverageDDAScans:
                    AverageDDAScans(scans, options);
                    break;

                case SpectraFileProcessingType.AverageDDAScansWithOverlap:
                    AverageDDAScans(scans, options);
                    break;
            }
        }

        public static void AverageAll(List<MsDataScan> scans, SpectralAveragingOptions options)
        {
            throw new NotImplementedException();
        }

        public static void AverageEverynScans(List<MsDataScan> scans, SpectralAveragingOptions options)
        {
            throw new NotImplementedException();
        }

        public static void AverageDDAScans(List<MsDataScan> scans, SpectralAveragingOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
