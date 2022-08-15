using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpectralAveraging;

namespace SpectralAveragingGUI
{
    public class DataStructureToStringConverter : BaseValueConverter<DataStructureToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value.ToString();
            if (((SpectraFileProcessingType[])Enum.GetValues(typeof(SpectraFileProcessingType))).Any(p => p.ToString() == value.ToString()))
            {
                if (val == "AverageEverynScans")
                    return "Average Every n Scans";
                else if (val == "AverageEverynScansWithOverlap")
                    return "Average Every n Scans with Overlap";
                else if (val == "AverageDDAScans")
                    return "Average DDA Scans";
                else if (val == "AverageDDAScansWithOverlap")
                    return "Average DDA Scans with Overlap";
            }

            if (string.IsNullOrWhiteSpace(val))
                    return "";
            StringBuilder newText = new StringBuilder(val.Length * 2);
            newText.Append(val[0]);
            for (int i = 1; i < val.Length; i++)
            {
                if (char.IsUpper(val[i]) && val[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(val[i]);
            }
            return new String(newText.ToString());

        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
