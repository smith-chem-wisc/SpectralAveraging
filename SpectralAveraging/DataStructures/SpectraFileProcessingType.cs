using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveraging
{
    public enum SpectraFileProcessingType
    {
        AverageAll,
        AverageEverynScans,
        AverageEverynScansWithOverlap,
        AverageDDAScans,
        AverageDDAScansWithOverlap,
    }
}
