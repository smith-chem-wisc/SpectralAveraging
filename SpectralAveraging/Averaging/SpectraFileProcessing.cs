using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AveragingIO;
using IO.MzML;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using MzLibUtil;

namespace SpectralAveraging
{
    /// <summary>
    /// Manages the spectra files being averaged
    /// </summary>
    public static class SpectraFileProcessing
    {
        #region Averaging
        public static void ProcessSpectra(List<MsDataScan> scans, SpectralAveragingOptions options, string spectraPath)
        {
            switch (options.SpectraFileProcessingType)
            {
                case SpectraFileProcessingType.AverageAll:
                    AverageAll(scans, options, spectraPath);
                    break;

                case SpectraFileProcessingType.AverageEverynScans:
                    AverageEverynScans(scans, options, spectraPath);
                    break;

                case SpectraFileProcessingType.AverageEverynScansWithOverlap:
                    AverageEverynScans(scans, options, spectraPath);
                    break;

                case SpectraFileProcessingType.AverageDDAScans:
                    AverageDDAScans(scans, options, spectraPath);
                    break;

                case SpectraFileProcessingType.AverageDDAScansWithOverlap:
                    AverageDDAScans(scans, options, spectraPath);
                    break;
            }
        }

        public static void AverageAll(List<MsDataScan> scans, SpectralAveragingOptions options, string spectraPath)
        {
            // average spectrum
            MsDataScan representativeScan = scans.First();
            MultiScanDataObject multiScanDataObject = new(SingleScanDataObject.ConvertMSDataScansInBulk(scans));
            MzSpectrum averagedSpectrum = SpectralMerging.CombineSpectra(multiScanDataObject, options);

            // create output
            MsDataScan averagedScan = new(averagedSpectrum, 1, representativeScan.OneBasedScanNumber,
                representativeScan.IsCentroid, representativeScan.Polarity, scans.Select(p => p.RetentionTime).Average(),
                averagedSpectrum.Range, null, representativeScan.MzAnalyzer, (double)multiScanDataObject.AverageIonCurrent,
                representativeScan.InjectionTime, null, representativeScan.NativeId);
            MsDataScan[] msDataScans = new MsDataScan[] { averagedScan };
            OutputAveragedScans(msDataScans, options, spectraPath);
        }

        public static void AverageEverynScans(List<MsDataScan> scans, SpectralAveragingOptions options, string spectraPath)
        {
            List<MsDataScan> averagedScans = new();
            int scanNumberIndex = 1;
            for (int i = 0; i < scans.Count; i += options.NumberOfScansToAverage - options.ScanOverlap)
            {
                // get the scans to be averaged
                List<MsDataScan> scansToProcess = new();
                if (i <= options.ScanOverlap) // very start of the file
                {
                    scansToProcess = scans.GetRange(i, options.NumberOfScansToAverage);
                }
                else if (i + options.NumberOfScansToAverage > scans.Count) // very end of the file
                {
                    break;
                }
                else // anywhere in the middle of the file
                {
                    scansToProcess = scans.GetRange(i , options.NumberOfScansToAverage);
                }

                // average scans
                MsDataScan representativeScan = scansToProcess.First();
                MultiScanDataObject multiScanDataObject = new(SingleScanDataObject.ConvertMSDataScansInBulk(scansToProcess));
                MzSpectrum averagedSpectrum = SpectralMerging.CombineSpectra(multiScanDataObject, options);
                MsDataScan averagedScan = new(averagedSpectrum, scanNumberIndex, 1,
                    representativeScan.IsCentroid, representativeScan.Polarity, scans.Select(p => p.RetentionTime).Average(),
                    averagedSpectrum.Range, null, representativeScan.MzAnalyzer, (double)multiScanDataObject.AverageIonCurrent,
                    scansToProcess.Select(p => p.InjectionTime).Average(), null, representativeScan.NativeId);

                averagedScans.Add(averagedScan);
                scanNumberIndex++;
            }

            OutputAveragedScans(averagedScans.ToArray(), options, spectraPath);

        }

        public static void AverageDDAScans(List<MsDataScan> scans, SpectralAveragingOptions options, string spectraPath)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Output

        public static void OutputAveragedScans(MsDataScan[] averagedScans, SpectralAveragingOptions options,
            string spectraPath)
        {
            switch (options.OutputType)
            {
                case OutputType.mzML:
                    OutputAveragedSpectraAsMzML(averagedScans, options, spectraPath);
                    break;

                case OutputType.txt:
                    OutputAveragedSpectraAsTxtFile(averagedScans, options, spectraPath);
                    break;

                default: throw new NotImplementedException("Output type not implemented");
            }

            if (options.OutputOptions)
            {
                string directoryPath = Path.GetDirectoryName(spectraPath);
                string optionsPath = Path.Combine(directoryPath, options.ToString() + ".txt");
                JsonSerializerDeserializer.SerializeToNewFile(options, optionsPath);
            }

        }

        public static void OutputAveragedSpectraAsMzML(MsDataScan[] averagedScans, SpectralAveragingOptions options, 
            string spectraPath)
        {
            SourceFile sourceFile = SpectraFileHandler.GetSourceFile(spectraPath);
            MsDataFile msDataFile = new(averagedScans, sourceFile);
            string spectraDirectory = Path.GetDirectoryName(spectraPath);
            string averagedPath = Path.Combine(spectraDirectory,
                "Averaged_" + Path.GetFileNameWithoutExtension(spectraPath) + options + ".mzML");

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(msDataFile, averagedPath, true);
        }

        public static void OutputAveragedSpectraAsTxtFile(MsDataScan[] averagedScans, SpectralAveragingOptions options,
            string spectraPath)
        {
            string spectraDirectory = Path.GetDirectoryName(spectraPath);
            if (options.SpectraFileProcessingType != SpectraFileProcessingType.AverageAll)
            {
                spectraDirectory = Path.Combine(spectraDirectory, "AveragedSpectra");
                if (!Directory.Exists(spectraDirectory))
                    Directory.CreateDirectory(spectraDirectory);
            }

            string averagedPath = Path.Combine(spectraDirectory,
                "Averaged_" + Path.GetFileNameWithoutExtension(spectraPath) + options + ".txt");

            foreach (var scan in averagedScans)
            {
                if (options.SpectraFileProcessingType != SpectraFileProcessingType.AverageAll)
                    averagedPath = Path.Combine(spectraDirectory,
                        "Averaged_" + Path.GetFileNameWithoutExtension(spectraPath) + "_" + scan.OneBasedScanNumber + options + ".txt");
                using (StreamWriter writer = new StreamWriter(File.Create(averagedPath)))
                {

                    for (int i = 0; i < scan.MassSpectrum.XArray.Length; i++)
                    {
                        writer.WriteLine(scan.MassSpectrum.XArray[i] + "\t" + scan.MassSpectrum.YArray[i]);
                    }
                }
            }
        }

        #endregion


    }
}
