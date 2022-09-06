using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using SpectralAveraging;

namespace AveragingIO
{
    public static class SpectralAveragingExtensions
    {
        public static MzSpectrum CombineSpectra(this MultiScanDataObject multiScanDataObject, SpectralAveragingOptions options)
        {
            double[][] compositeSpectraValues = SpectralMerging.CombineSpectra(multiScanDataObject.XArrays, multiScanDataObject.YArrays, multiScanDataObject.TotalIonCurrent,
                multiScanDataObject.ScansToProcess, options);
            return new MzSpectrum(compositeSpectraValues[0], compositeSpectraValues[1], true);
        }

        /// <summary>
        /// Normalization method overload for MultiScanDataObjects
        /// </summary>
        /// <param name="scans"></param>
        public static void NormalizeSpectrumToTic(this MultiScanDataObject scans, bool multiplyByAverageTic)
        {
            for (int i = 0; i < scans.YArrays.GetLength(0); i++)
            {
                if (multiplyByAverageTic && scans.AverageIonCurrent != null)
                {
                    SpectrumNormalization.NormalizeSpectrumToTic(scans.YArrays[i], scans.TotalIonCurrent[i], (double)scans.AverageIonCurrent);
                }
                else
                {
                    SpectrumNormalization.NormalizeSpectrumToTic(scans.YArrays[i], scans.TotalIonCurrent[i]);
                }
            }
        }

        /// <summary>
        /// Normalization method overload for SingleScanDataObjects
        /// </summary>
        /// <param name="scan"></param>
        public static void NormalizeSpectrumToTic(this SingleScanDataObject scan)
        {
            SpectrumNormalization.NormalizeSpectrumToTic(scan.YArray, scan.TotalIonCurrent);
        }

    }
}
