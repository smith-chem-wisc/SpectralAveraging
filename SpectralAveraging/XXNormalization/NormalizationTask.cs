using SpectralAveraging.Averaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging
{
    internal class NormalizationTask : Task
    {
        public override void RunTask<T, U>(T options, U data)
        {
            if (data == null || options == null)
            {
                throw new ArgumentException("Arguments cannot be null");
            }
            if (typeof(T) != typeof(NormalizationOptions))
            {
                throw new ArgumentException("Invalid options class for NormalizationTask");
            }
            if ((options as NormalizationOptions).PerformNormalization == true)
            {
                if (typeof(U) == typeof(MultiScanDataObject))
                {
                    SpectrumNormalization.NormalizeSpectrumToTic(data as MultiScanDataObject, true);
                }
                else if (typeof(U) == typeof(SingleScanDataObject))
                {
                    SpectrumNormalization.NormalizeSpectrumToTic(data as SingleScanDataObject);
                }
            }
        }
    }
}
