using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.MzML;
using MassSpectrometry;
using SpectralAveraging;


namespace Tests
{
    public class TestNormalization
    {
        [Test]
        public static void TestBaseNormalizationFunctions()
        {
            double[] sampleData = new double[] { 100, 80, 70, 60, 50, 40 };
            double[] expected = new double[] { 0.25, 0.2, 0.175, 0.15, 0.125, 0.1 };
            SpectrumNormalization.NormalizeSpectrumToTic(ref sampleData, 400);
            Assert.That(sampleData.Sum() == 1);
            Assert.That(sampleData.SequenceEqual(expected));

            sampleData = new double[] { 100, 80, 70, 60, 50, 40 };
            SpectrumNormalization.NormalizeSpectrumToTic(sampleData, 400);
            Assert.That(sampleData.Sum() == 1);
            Assert.That(sampleData.SequenceEqual(expected));
        }

        [Test]
        public static void TestNormalizationWithDifferentDataTypes()
        {
            // setup
            string filepath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"DataFiles\TDYeastFractionMS1.mzML");
            List<MsDataScan> scans = Mzml.LoadAllStaticData(filepath).GetAllScansList();

            // single scan data object
            MsDataScan firstScan = scans.First();
            double[] yArray = firstScan.MassSpectrum.YArray;
            yArray = yArray.Select(p => p / firstScan.TotalIonCurrent).ToArray();
            SpectrumNormalization.NormalizeSpectrumToTic(firstScan.MassSpectrum.YArray, firstScan.TotalIonCurrent);
            Assert.That(firstScan.MassSpectrum.YArray.SequenceEqual(yArray));

            // multi scan data object
            MultiScanDataObject multiScan = new MultiScanDataObject(singleScans.GetRange(1, 5));
            double[][] yArrays = multiScan.YArrays;
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                yArrays[i] = multiScan.YArrays[i].Select(p => p / multiScan.TotalIonCurrent[i]).ToArray();
            }
            multiScan.NormalizeSpectrumToTic(false);
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                Assert.That(multiScan.YArrays[i].SequenceEqual(yArrays[i]));
            }
        }
    }
}
