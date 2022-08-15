using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpectralAveraging;

namespace SpectralAveragingGUI
{
    public class SpectraFileProcessingTypeToOpacityOverlapConverter : BaseValueConverter<SpectraFileProcessingTypeToOpacityOverlapConverter>
    {
        private double doNotDisplay = 0.5;

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == typeof(SpectraFileProcessingType))
            {
                switch (value)
                {
                    case SpectraFileProcessingType.AverageAll:
                        return doNotDisplay;

                    case SpectraFileProcessingType.AverageEverynScans:
                        return doNotDisplay;

                    case SpectraFileProcessingType.AverageEverynScansWithOverlap:
                        return 1;

                    case SpectraFileProcessingType.AverageDDAScans:
                        return doNotDisplay;

                    case SpectraFileProcessingType.AverageDDAScansWithOverlap:
                        return 1;
                }
            }

            return 1;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
