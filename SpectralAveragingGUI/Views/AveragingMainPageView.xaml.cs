using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpectralAveragingGUI
{
    /// <summary>
    /// Interaction logic for AveragingMainPageControl.xaml
    /// </summary>
    public partial class AveragingMainPageView : UserControl
    {
        public AveragingMainPageView()
        {
            InitializeComponent();
        }

        private void Selector_OnSelected(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void DataGrid_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            string[] addedSpectra = e.AddedCells.Select(p => p.Item.ToString()).ToArray();
            string[] removedSpectra = e.RemovedCells.Select(p => p.Item.ToString()).ToArray();
            ((AveragingMainPageViewModel)DataContext).SelectedSpectraChanged(addedSpectra, removedSpectra);
        }
    }
}
