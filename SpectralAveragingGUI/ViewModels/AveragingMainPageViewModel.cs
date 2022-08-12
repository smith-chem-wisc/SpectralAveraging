using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AveragingIO;
using System.Windows;
using System.Windows.Input;
using SpectralAveraging;

namespace SpectralAveragingGUI
{
    /// <summary>
    /// ViewModel to provide data to the AverageMainPageView
    /// </summary>
    public class AveragingMainPageViewModel : BaseViewModel, IFileDragDropTarget
    {


        #region Private Members

        private List<string> spectraFilePaths;
        private List<string> selectedSpectra;
        private AveragingOptionsViewModel averagingOptionsViewModel;
        private string defaultOptionsDirectoryPath = Path.Combine(SpectralAveragingGUIGlobalSettings.DataDirectory,
            @"DefaultOptions");

        #endregion

        #region Public Properties

        /// <summary>
        /// List of spectra file paths to be displayed in the SpectraFileBorder
        /// </summary>
        public List<string> SpectraFilePaths
        {
            get { return spectraFilePaths; }
            set { spectraFilePaths = value; OnPropertyChanged(nameof(SpectraFilePaths));
                OnPropertyChanged(nameof(SpectraNames)); }   
        }

        /// <summary>
        /// List of spectra file paths without their extensions
        /// </summary>
        public List<string> SpectraNames
        {
            get{ return spectraFilePaths.Select(p => p.Split("\\")[p.Split("\\").Length - 1]).ToList(); }
        }

        /// <summary>
        /// List of spectra that are selected in the displayed list
        /// </summary>
        public List<string> SelectedSpectra
        {
            get { return selectedSpectra; }
            set { selectedSpectra = value;
                OnPropertyChanged(nameof(SelectedSpectra)); }
        }

        public AveragingOptionsViewModel AveragingOptionsViewModel
        {
            get { return averagingOptionsViewModel; }
            set { averagingOptionsViewModel = value; OnPropertyChanged(nameof(AveragingOptionsViewModel)); }
        }

        #endregion

        #region Commands
        public ICommand AddSpectraCommand { get; set; }
        public ICommand RemoveSpectraCommand { get; set; }
        public ICommand RemoveAllSpectraCommand { get; set; }
        public ICommand AverageSpectraCommand { get; set; }
        public ICommand SaveAsDefaultCommand { get; set; }
        public ICommand ResetDefaultsCommand { get; set; }

        #endregion

        #region Constructor

        public AveragingMainPageViewModel()
        {
            // value initialization
            spectraFilePaths = new();
            selectedSpectra = new();


            SpectralAveragingOptions options;
            string optionsPath = Path.Combine(defaultOptionsDirectoryPath, "defaultOptions.txt");
            if (File.Exists(optionsPath))
            {
                options = JsonSerializerDeserializer.Deserialize<SpectralAveragingOptions>(optionsPath, true);
            }
            else
            {
                options = new();
            }
            AveragingOptionsViewModel = new AveragingOptionsViewModel(options);

            // command assignment
            AddSpectraCommand = new RelayCommand(() => AddSpectra());
            RemoveSpectraCommand = new RelayCommand(() => RemoveSpectra()); 
            RemoveAllSpectraCommand = new RelayCommand(() => RemoveAllSpectra());
            AverageSpectraCommand = new RelayCommand(() => AverageSpectra());
            SaveAsDefaultCommand = new RelayCommand(() => SaveAsDefault());
            ResetDefaultsCommand = new RelayCommand(() => ResetDefaults());
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Method ran when the AddSpectraButton is clicked
        /// </summary>
        private void AddSpectra(string[] files = null)
        {
            if (files == null)
            {
                // open file finder dialog
                string filterString = string.Join(";", SpectralAveragingGUIGlobalSettings.AcceptableSpectraFileTypes.Select(p => "*" + p));
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Spectra Files(" + filterString + ")|" + filterString,
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                    files = openFileDialog.FileNames;
                else
                    return;
            }
            
            // Check file type, load if acceptable
            List<string> errors = new();
            foreach (var file in files)
            {
                if (SpectralAveragingGUIGlobalSettings.AcceptableSpectraFileTypes.Any(p => file.Contains(p)))
                {
                    SpectraFilePaths.Add(file);
                }
                else
                {
                    errors.Add(file);
                }
            }

            // Display errors
            if (errors.Any())
            {
                StringBuilder sb = new();
                sb.AppendLine("Files not in correct format");
                sb.AppendLine("Acceptable file formats are " + String.Join(',', SpectralAveragingGUIGlobalSettings.AcceptableSpectraFileTypes));
                sb.AppendLine("");
                foreach (var error in errors)
                {
                    sb.AppendLine(error);
                }
                MessageBox.Show(sb.ToString());
            }

            // Update Visual Representation
            OnPropertyChanged(nameof(SpectraFilePaths));
            OnPropertyChanged(nameof(SpectraNames));
            
        }

        /// <summary>
        /// Method ran when the RemoveSpectraButton is clicked
        /// </summary>
        private void RemoveSpectra()
        {
            foreach (var spectrumToRemove in SelectedSpectra)
            {
                SpectraFilePaths.Remove(SpectraFilePaths.First(p => p.Contains(spectrumToRemove)));
            }
            OnPropertyChanged(nameof(SpectraFilePaths));
            OnPropertyChanged(nameof(SpectraNames));
        }

        /// <summary>
        /// Method ran when the RemoveAllSpectraButton is clicked
        /// </summary>
        private void RemoveAllSpectra()
        {
            spectraFilePaths = new();
            OnPropertyChanged(nameof(SpectraFilePaths));
            OnPropertyChanged(nameof(SpectraNames));
        }

        /// <summary>
        /// Initiates Spectral Averaging code
        /// </summary>
        private void AverageSpectra()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves currently selected options as default
        /// </summary>
        private void SaveAsDefault()
        {
            if (!Directory.Exists(defaultOptionsDirectoryPath))
                Directory.CreateDirectory(defaultOptionsDirectoryPath);

            string optionsPath = Path.Combine(defaultOptionsDirectoryPath, "defaultOptions.txt");
            JsonSerializerDeserializer.SerializeToNewFile(AveragingOptionsViewModel.SpectralAveragingOptions, optionsPath);
        }

        /// <summary>
        /// Resets saved options to standard values
        /// </summary>
        private void ResetDefaults()
        {
            string optionsPath = Path.Combine(defaultOptionsDirectoryPath, "defaultOptions.txt");
            if (File.Exists(optionsPath))
                File.Delete(optionsPath);
            AveragingOptionsViewModel.ResetDefaults();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Determines behavior when files are dropped into specified area, also used to add file to 
        /// </summary>
        /// <param name="filepaths">paths of files dropped</param>
        public void OnFileDrop(string[] filepaths)
        {
            AddSpectra(filepaths);
        }

        public void SelectedSpectraChanged(string[] addedSpectra, string[] removedSpectra)
        {
            foreach (var spectrum in addedSpectra)
            {
                SelectedSpectra.Add(SpectraFilePaths.First(p => p.Contains(spectrum)));
            }

            foreach (var spectrum in removedSpectra)
            {
                SelectedSpectra.Remove(SpectraFilePaths.First(p => p.Contains(spectrum)));
            }

            OnPropertyChanged(nameof(SelectedSpectra));
        }

  

        #endregion


    }
}
