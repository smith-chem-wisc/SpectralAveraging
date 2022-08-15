﻿using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;

namespace AveragingIO
{
	public static class SpectraFileHandler
	{
		/// <summary>
		/// Creates a List of MsDataScans from a spectra file. Currently supports MzML and raw
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		/// <exception cref="MzLibException"></exception>
		public static List<MsDataScan> LoadAllScansFromFile(string filepath)
		{
			List<MsDataScan> scans = new();
            if (filepath.EndsWith(".mzML"))
            {
                var temp = Mzml.LoadAllStaticData(filepath);
                scans = temp.GetAllScansList();
			}
            else if (filepath.EndsWith(".raw"))
            {
                var temp = ThermoRawFileReader.LoadAllStaticData(filepath);
                scans = temp.GetAllScansList();
			}
            else
			{
				throw new ArgumentException("Cannot load spectra");
			}
			return scans;
		}

		/// <summary>
		/// Gets the source file for the spectra file at designated path
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="filepath"></param>
		/// <returns></returns>
		/// <exception cref="MzLibException"></exception>
        public static SourceFile GetSourceFile(string filepath)
        {
            List<MsDataScan> scans = new();
            if (filepath.EndsWith(".mzML"))
            {
                return Mzml.LoadAllStaticData(filepath).SourceFile;
            }
            else if (filepath.EndsWith(".raw"))
            {
                return ThermoRawFileReader.LoadAllStaticData(filepath).SourceFile;
            }
            else
            {
                throw new ArgumentException("Cannot access SourceFile");
            }

		}

		/// <summary>
		/// Creates a List of MsDataScans from a spectra file
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="start">OneBasedScanNumber of the first scan</param>
		/// <param name="end">Optional: will return only one scan if blank</param>
		/// <returns></returns>
		public static List<MsDataScan> LoadSelectScansFromFile(string filepath, int start, int end = -1)
		{
			if (end == -1)
			{
				end = start + 1;
			}
			List<MsDataScan> scans = LoadAllScansFromFile(filepath);
			List<MsDataScan> trimmedScans = scans.GetRange(start - 1, (end - start));
			return trimmedScans;
		}

		/// <summary>
		/// returns the MS1's only from a file
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public static List<MsDataScan> LoadMS1ScansFromFile(string filepath)
		{
			return LoadAllScansFromFile(filepath).Where(p => p.MsnOrder == 1).ToList();
		}

		/// <summary>
		/// returns MS1's only from a SID scan file
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public static List<MsDataScan> LoadMS1ScanFromFile(string filepath)
		{
			return LoadAllScansFromFile(filepath).Where(p => p.OneBasedScanNumber % 2 == 1).ToList();
		}

		public static List<(double, double)> GetMzandIntValuesFromSingleScan(MsDataScan scan)
		{
			List<(double, double)> result = new List<(double, double)>();
			MzSpectrum spectrum = scan.MassSpectrum;
			for (int i = 0; i < spectrum.XArray.Length; i++)
			{
				result.Add((spectrum.XArray[i], spectrum.YArray[i]));
			}
			return result;
		}




		// TODO: Make it so the list of combined scans can be exported as an openable Mzml
		public static void SaveMergedScanAsMzml(List<MsDataScan> combinedScans, string outputPath)
		{
			SourceFile temp = new SourceFile(null, null, null, null, null);
			MsDataFile combinedScansFile = new MsDataFile(combinedScans.ToArray(), temp);
			MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(combinedScansFile, outputPath, false);
		}
	}
}