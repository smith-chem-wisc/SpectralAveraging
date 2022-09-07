using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MassSpectrometry;
using Nett;
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
        private ObservableCollection<AveragingOptionsViewModel> savedAveragingOptions;
        private ObservableCollection<AveragingOptionsViewModel> selectedOptions;
        private AveragingOptionsViewModel averagingOptionsViewModel;
        private string savedOptionsDirectoryPath = Path.Combine(SpectralAveragingGUIGlobalSettings.DataDirectory,
            @"SavedOptions");
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

        public ObservableCollection<AveragingOptionsViewModel> SavedAveragingOptions
        {
            get => savedAveragingOptions;
            set { savedAveragingOptions = value; OnPropertyChanged(nameof(SavedAveragingOptions)); }
        }

        public List<string> OptionsNames
        {
            get => savedAveragingOptions.Select(p => p.Name).ToList();
        }

        public ObservableCollection<AveragingOptionsViewModel> SelectedOptions
        {
            get => selectedOptions;
            set { selectedOptions = value; OnPropertyChanged(nameof(SelectedOptions)); }
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
        public ICommand SaveOptionsCommand { get; set; }
        public ICommand ResetDefaultsCommand { get; set; }
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public ICommand AddOptionsCommand { get; set; }
        public ICommand RemoveOptionsCommand { get; set; }
        public ICommand RemoveAllOptionsCommand { get; set; }
        public ICommand OptionsDoubleClickedCommand { get; set; }
        public ICommand CreateNewDefaultOptionsCommand { get; set; }

        #endregion

        #region Constructor

        public AveragingMainPageViewModel()
        {
            // value initialization
            spectraFilePaths = new();
            selectedSpectra = new();
            savedAveragingOptions = new();
            selectedOptions = new();
            errors = new();
            progressBarVisisbilty = false;
            stopwatch = new();
            worker = new BackgroundWorker() { WorkerReportsProgress = true };

            SpectralAveragingOptions options;
            if (Directory.Exists(savedOptionsDirectoryPath))
            {
                foreach (var file in Directory.EnumerateFiles(savedOptionsDirectoryPath))
                {
                    SpectralAveragingOptions option = Toml.ReadFile<SpectralAveragingOptions>(file);
                    savedAveragingOptions.Add(new AveragingOptionsViewModel(option) {SavedPath = file});
                }

                if (savedAveragingOptions.Count > 0)
                    AveragingOptionsViewModel = savedAveragingOptions.First();
                else
                    AveragingOptionsViewModel = new(new SpectralAveragingOptions());
                
            }
            else
            {
                AveragingOptionsViewModel = new AveragingOptionsViewModel(new SpectralAveragingOptions());
            }

            // if no options are saved, create a default
            if (savedAveragingOptions.Count == 0)
            {
                savedAveragingOptions.Add(AveragingOptionsViewModel);
                string optionsPath = Path.Combine(savedOptionsDirectoryPath, AveragingOptionsViewModel.Name + ".toml");
                AveragingOptionsViewModel.SavedPath = optionsPath;
                Toml.WriteFile(AveragingOptionsViewModel.SpectralAveragingOptions, optionsPath);
            }
                

            // command assignment
            AddSpectraCommand = new RelayCommand(AddSpectra);
            RemoveSpectraCommand = new RelayCommand(RemoveSpectra); 
            RemoveAllSpectraCommand = new RelayCommand(RemoveAllSpectra);
            AverageSpectraCommand = new RelayCommand(AverageSpectra);
            SaveOptionsCommand = new RelayCommand(SaveOptions);
            ResetDefaultsCommand = new RelayCommand(ResetDefaults);
            AddOptionsCommand = new RelayCommand(AddOptions);
            RemoveOptionsCommand = new RelayCommand(RemoveOptions);
            RemoveAllOptionsCommand = new RelayCommand(RemoveAllOptions);
            OptionsDoubleClickedCommand = new RelayCommand(OptionsDoubleClicked);
            CreateNewDefaultOptionsCommand = new RelayCommand(CreateNewDefaultOption);
            worker.DoWork += Worker_AverageSpectra;
            worker.ProgressChanged += Worker_ReportProgress;
            worker.RunWorkerCompleted += Worker_RunWorkerComplete;
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Method ran when the AddSpectraButton is clicked
        /// </summary>
        private void AddSpectra()
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
            {
                string[] files = openFileDialog.FileNames;
                OnFileDrop(files);
            }
            else
                return;
            
            UpdateFileRelatedFields();
        }

        /// <summary>
        /// Method ran when the RemoveSpectraButton is clicked
        /// </summary>
        private void RemoveSpectra()
        {
            if (SelectedSpectra.Count > 0)
            {
                for (int i = SelectedSpectra.Count; i > 0; i--)
                {
                    SpectraFilePaths.Remove(SpectraFilePaths.First(p => p.Contains(SelectedSpectra[i - 1])));
                    SelectedSpectra.Remove(SelectedSpectra[i - 1]);
                }

                UpdateFileRelatedFields();
            }
        }

        /// <summary>
        /// Method ran when the RemoveAllSpectraButton is clicked
        /// </summary>
        private void RemoveAllSpectra()
        {
            spectraFilePaths = new();
            SelectedSpectra.Clear();
            UpdateFileRelatedFields();
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
        private void SaveOptions()
        {
            if (!Directory.Exists(savedOptionsDirectoryPath))
                Directory.CreateDirectory(savedOptionsDirectoryPath);

            string name = AveragingOptionsViewModel.Name;
            var optionsNameDialog = new TextResponseDialogWindow() {ResponseText = name};
            if (optionsNameDialog.ShowDialog() == true)
            {
                AveragingOptionsViewModel.DeleteOptions();
                SavedAveragingOptions.Remove(AveragingOptionsViewModel);
                SelectedOptions.Remove(AveragingOptionsViewModel);
                string optionsPath = Path.Combine(savedOptionsDirectoryPath, optionsNameDialog.ResponseText + ".toml");
                AveragingOptionsViewModel.SavedPath = optionsPath;
                AveragingOptionsViewModel.SaveOptions();
                OnFileDrop(new string[] { optionsPath});
            }
            UpdateFileRelatedFields();
        }

        /// <summary>
        /// Resets saved options to standard values
        /// </summary>
        private void ResetDefaults()
        {
            string optionsPath = Path.Combine(savedOptionsDirectoryPath, "defaultOptions.txt");
            if (File.Exists(optionsPath))
                File.Delete(optionsPath);
            AveragingOptionsViewModel.ResetDefaults();
        }

        private void AddOptions()
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
            {
                string[] files = openFileDialog.FileNames;
                OnFileDrop(files);
            }
            else
                return;

            UpdateFileRelatedFields();
        }

        private void RemoveOptions()
        {
            if (SelectedOptions.Count > 0)
            {
                for (int i = SelectedOptions.Count; i > 0; i--)
                {
                    SavedAveragingOptions.Remove(SavedAveragingOptions.First(p => p.Name.Equals(SelectedOptions[i - 1].Name)));
                    SelectedOptions[i - 1].DeleteOptions();
                    SelectedOptions.Remove(SelectedOptions[i - 1]);
                }

                if (SavedAveragingOptions.Count > 0)
                {
                    AveragingOptionsViewModel = SavedAveragingOptions.First();
                }
                else
                {
                    AveragingOptionsViewModel = new(new SpectralAveragingOptions());
                }

                AveragingOptionsViewModel.UpdateVisualRepresentation();
                UpdateFileRelatedFields();
            }
        }

        private void RemoveAllOptions()
        {
            SavedAveragingOptions.Clear();
            UpdateFileRelatedFields();

            foreach (var option in SavedAveragingOptions)
            {
                option.DeleteOptions();
            }

            AveragingOptionsViewModel = new(new SpectralAveragingOptions());
            AveragingOptionsViewModel.UpdateVisualRepresentation();
        }

        public void OptionsDoubleClicked()
        {
            if (SelectedOptions.Count == 1)
            {
                AveragingOptionsViewModel = SelectedOptions.First();
                UpdateFileRelatedFields();
                AveragingOptionsViewModel.UpdateVisualRepresentation();
            }
            else
            {
                // alert developer of error
                Debugger.Break();
            }
        }

        private void CreateNewDefaultOption()
        {
            SpectralAveragingOptions options = new();
            AveragingOptionsViewModel optionsViewModel = new(options);
            optionsViewModel.SavedPath = Path.Combine(savedOptionsDirectoryPath, optionsViewModel.Name + ".toml");
            AveragingOptionsViewModel = optionsViewModel;
            AveragingOptionsViewModel.SaveOptions();
            savedAveragingOptions.Add(AveragingOptionsViewModel);
            UpdateFileRelatedFields();
            AveragingOptionsViewModel.UpdateVisualRepresentation();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Determines behavior when files are dropped into specified area, also used to add file to 
        /// </summary>
        /// <param name="filepaths">paths of files dropped</param>
        public void OnFileDrop(string[] filepaths)
        {
            List<string> errors = new();
            foreach (var path in filepaths)
            {
                string extension = Path.GetExtension(path);
                if (extension == ".mzML" || extension == ".raw")
                {
                    // Check file type, load if acceptable
                    if (SpectralAveragingGUIGlobalSettings.AcceptableSpectraFileTypes.Any(p => path.Contains(p)))
                    {
                        SpectraFilePaths.Add(path);
                    }
                    else
                    {
                        errors.Add(path);
                    }
                }

                if (extension == ".toml")
                {
                    try
                    {
                        SpectralAveragingOptions option = Toml.ReadFile<SpectralAveragingOptions>(path);
                        string optionsPath = Path.Combine(savedOptionsDirectoryPath, Path.GetFileNameWithoutExtension(path) + ".toml");
                        AveragingOptionsViewModel optionsViewModel = new AveragingOptionsViewModel(option) { SavedPath = optionsPath };
                        SavedAveragingOptions.Add(optionsViewModel);
                        if (!File.Exists(optionsPath))
                            optionsViewModel.SaveOptions();
                    }
                    catch (Exception e)
                    {
                        errors.Add("Options file " + Path.GetFileNameWithoutExtension(path) + " could not load properly");
                    }
                }
            }

            // Display errors
            if (errors.Any())
            {
                StringBuilder sb = new();
                sb.AppendLine("Files not in correct format");
                sb.AppendLine("Acceptable file formats are " + String.Join(", ", SpectralAveragingGUIGlobalSettings.AcceptableSpectraFileTypes));
                sb.AppendLine("");
                foreach (var error in errors)
                {
                    sb.AppendLine(error);
                }
                MessageBox.Show(sb.ToString());
            }

            // Update Visual Representation
            UpdateFileRelatedFields();
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

        public void SelectedOptionsChanged(string[] addedOptions, string[] removedOptions)
        {
            foreach (var option in addedOptions)
            {
                SelectedOptions.Add(SavedAveragingOptions.First(p => p.Name.Contains(option)));
            }

            foreach (var option in removedOptions)
            {
                if (option == "{DependencyProperty.UnsetValue}")
                    continue;
                SelectedOptions.Remove(SavedAveragingOptions.First(p => p.Name.Contains(option)));
            }
        }

        /// <summary>
        /// Updates the visual representation of the fields that have to do with spectra file handling
        /// </summary>
        private void UpdateFileRelatedFields()
        {
            OnPropertyChanged(nameof(SpectraFilePaths));
            OnPropertyChanged(nameof(SpectraNames));
            OnPropertyChanged(nameof(SavedAveragingOptions));
            OnPropertyChanged(nameof(OptionsNames));
        }
        

        #endregion


    }
}
