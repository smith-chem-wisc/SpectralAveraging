using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveragingGUI
{
    public static class SpectralAveragingGUIGlobalSettings
    {
        public static string[] AcceptableSpectraFileTypes { get; set; } = { ".mzML", ".raw" };
        public static string DataDirectory { get; set; }

        static SpectralAveragingGUIGlobalSettings()
        {
            DataDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }


    }
}
