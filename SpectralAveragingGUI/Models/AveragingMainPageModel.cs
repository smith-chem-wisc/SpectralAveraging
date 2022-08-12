using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectralAveragingGUI
{
    /// <summary>
    /// Model for design time representation of the AveragingPage
    /// </summary>
    public class AveragingMainPageModel : AveragingMainPageViewModel
    {
        public static AveragingMainPageModel Instance => new AveragingMainPageModel();

        public AveragingMainPageModel() : base()
        {
            string path1 = @"D:\Projects\Top Down MetaMorpheus\RawSpectra\FXN4_tr1_032017.raw";
            string path2 = @"D:\Projects\Top Down MetaMorpheus\RawSpectra\FXN5_tr1_032017.mzML";
            SpectraFilePaths.Add(path1);
            SpectraFilePaths.Add(path2);
            SpectraFilePaths.Add(@"D:\Projects\Top Down MetaMorpheus\RawSpectra\FXN6_tr1_032017.raw");
            SpectraFilePaths.Add(@"D:\Projects\Top Down MetaMorpheus\RawSpectra\FXN7_tr1_032017.raw");

            SelectedSpectra.Add(path1);
            SelectedSpectra.Add(path2);
        }

    }
}
