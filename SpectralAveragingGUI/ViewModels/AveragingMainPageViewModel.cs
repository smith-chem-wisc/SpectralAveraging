using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AveragingIO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MassSpectrometry;
using SpectralAveraging;

namespace SpectralAveragingGUI
{
    /// <summary>
    /// ViewModel to provide data to the AverageMainPageView
    /// </summary>
    public class AveragingMainPageViewModel : BaseViewModel, IFileDragDropTarget
    {


        #region Private Members

        private int spectraProcessingComblete;
        private ObservableCollection<string> spectraFilePaths;
        private ObservableCollection<string> selectedSpectra;
        private AveragingOptionsViewModel averagingOptionsViewModel;
        private string defaultOptionsDirectoryPath = Path.Combine(SpectralAveragingGUIGlobalSettings.DataDirectory,
            @"DefaultOptions");
        private bool progressBarVisisbilty;
        private List<string> errors;
        private readonly BackgroundWorker worker;
        private string processingText;
        private Stopwatch stopwatch;

        #endregion

        #region Public Properties

        /// <summary>
        /// List of spectra file paths to be displayed in the SpectraFileBorder
        /// </summary>
        public ObservableCollection<string> SpectraFilePaths
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
        public ObservableCollection<string> SelectedSpectra
        {
            get { return selectedSpectra; }
            set { selectedSpectra = value; OnPropertyChanged(nameof(SelectedSpectra)); }
        }

        /// <summary>
        /// visual representation of the averaging options
        /// </summary>
        public AveragingOptionsViewModel AveragingOptionsViewModel
        {
            get { return averagingOptionsViewModel; }
            set { averagingOptionsViewModel = value; OnPropertyChanged(nameof(AveragingOptionsViewModel)); }
        }

        /// <summary>
        /// The number of spectra where processing is complete, for progress bar display
        /// </summary>
        public int SpectraProcessingComplete
        {
            get { return spectraProcessingComblete; }
            set { spectraProcessingComblete = value; OnPropertyChanged(nameof(SpectraProcessingComplete)); }
        }

        public bool ProgressBarVisibility
        {
            get { return progressBarVisisbilty; }
            set { progressBarVisisbilty = value; OnPropertyChanged(nameof(ProgressBarVisibility)); }
        }

        public string ProcessingText
        {
            get { return processingText; }
            set { processingText = value; OnPropertyChanged(nameof(ProcessingText)); }
        }

        public bool IsAveraging
        {
            get { return worker.IsBusy; }
        }

        #endregion

        #region Commands
        public ICommand AddSpectraCommand { get; set; }
        public ICommand RemoveSpectraCommand { get; set; }
        public ICommand RemoveAllSpectraCommand { get; set; }
        public ICommand AverageSpectraCommand { get; set; }
        public ICommand SaveAsDefaultCommand { get; set; }
        public ICommand ResetDefaultsCommand { get; set; }
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        #endregion

        #region Constructor

        public AveragingMainPageViewModel()
        {
            // value initialization
            spectraFilePaths = new();
            selectedSpectra = new();
            errors = new();
            progressBarVisisbilty = false;
            stopwatch = new();
            worker = new BackgroundWorker() { WorkerReportsProgress = true };

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
            worker.DoWork += Worker_AverageSpectra;
            worker.ProgressChanged += Worker_ReportProgress;
            worker.RunWorkerCompleted += Worker_RunWorkerComplete;
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
            UpdateSpectraFileRelatedFields();
        }

        /// <summary>
        /// Method ran when the RemoveSpectraButton is clicked
        /// </summary>
        private void RemoveSpectra()
        {
            for (int i = SelectedSpectra.Count; i > 0; i--)
            {
                SpectraFilePaths.Remove(SpectraFilePaths.First(p => p.Contains(SelectedSpectra[i - 1])));
                SelectedSpectra.Remove(SelectedSpectra[i - 1]);
            }

            UpdateSpectraFileRelatedFields();
        }

        /// <summary>
        /// Method ran when the RemoveAllSpectraButton is clicked
        /// </summary>
        private void RemoveAllSpectra()
        {
            spectraFilePaths = new();
            SelectedSpectra.Clear();
            UpdateSpectraFileRelatedFields();
        }

        /// <summary>
        /// Initiates Spectral Averaging code
        /// </summary>
        private void AverageSpectra()
        {
            List<string> errors = new();
            ProgressBarVisibility = true;
            ProcessingText = "Starting Averaging";
            errors.Clear();
            worker.RunWorkerAsync();
            OnPropertyChanged(nameof(IsAveraging));

            if (errors.Any())
            {
                StringBuilder sb = new();
                sb.AppendLine("Errors occurred while attempting to average the following files:");
                foreach (var error in errors)
                {
                    sb.AppendLine(error);
                }

                MessageBox.Show(sb.ToString());
            }
        }

        private void Worker_AverageSpectra(object sender, DoWorkEventArgs e)
        {
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < SpectraFilePaths.Count; i++)
            {
                try
                {
                    List<MsDataScan> scans = SpectraFileHandler.LoadAllScansFromFile(SpectraFilePaths[i]);
                    MsDataScan[] averagedScans = SpectraFileProcessing.ProcessSpectra(scans, AveragingOptionsViewModel.SpectralAveragingOptions);
                    AveragedSpectraOutputter.OutputAveragedScans(averagedScans, AveragingOptionsViewModel.SpectralAveragingOptions, SpectraFilePaths[i]);
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileNameWithoutExtension(SpectraFilePaths[i]) + ": " + ex.Message);
                }
                worker.ReportProgress(i + 1);
            }
            stopwatch.Stop();
        }

        private void Worker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            SpectraProcessingComplete = e.ProgressPercentage;
            if (e.ProgressPercentage == SpectraFilePaths.Count)
            {
                ProcessingText = $"Averaging Finished - Elapsed Time: {stopwatch.Elapsed}";
            }
            else
            {
                ProcessingText = $"Averaged {SpectraProcessingComplete}/{SpectraFilePaths.Count} Spectra";
            }
        }

        private void Worker_RunWorkerComplete(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsAveraging));
            worker.Dispose();
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
                if (spectrum == "{DependencyProperty.UnsetValue}")
                    continue;
                SelectedSpectra.Remove(SpectraFilePaths.First(p => p.Contains(spectrum)));
            }

            OnPropertyChanged(nameof(SelectedSpectra));
        }

        /// <summary>
        /// Updates the visual representation of the fields that have to do with spectra file handling
        /// </summary>
        private void UpdateSpectraFileRelatedFields()
        {
            OnPropertyChanged(nameof(SpectraFilePaths));
            OnPropertyChanged(nameof(SpectraNames));
        }
        

        #endregion


    }
}
