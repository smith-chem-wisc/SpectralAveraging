using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpectralAveragingGUI
{
    public class IntegerTextBox : TextBox
    {
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            foreach (var character in e.Text)
            {
                if (!Char.IsDigit(character))
                {
                    e.Handled = true;
                    return;
                }
            }
            e.Handled = false;
        }
    }
}
