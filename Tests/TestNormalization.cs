using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Normalization;
using InstrumentControl;
using MassSpectrometry;
using InstrumentControlIO;
using Data;
using Standardization;

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
            List<MsDataScan> scans = SpectraFileHandler.LoadMS1ScansFromFile(filepath);
            List<SingleScanDataObject> singleScans = SingleScanDataObject.ConvertMSDataScansInBulk(scans);

            // single scan data object
            SingleScanDataObject singleScan = singleScans.First();
            double[] yArray = singleScan.YArray;
            yArray = yArray.Select(p => p / singleScan.TotalIonCurrent).ToArray();
            SpectrumNormalization.NormalizeSpectrumToTic(singleScan);
            Assert.That(singleScan.YArray.SequenceEqual(yArray));

            // multi scan data object
            MultiScanDataObject multiScan = new MultiScanDataObject(singleScans.GetRange(1, 5));
            double[][] yArrays = multiScan.YArrays;
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                yArrays[i] = multiScan.YArrays[i].Select(p => p / multiScan.TotalIonCurrent[i]).ToArray();
            }
            SpectrumNormalization.NormalizeSpectrumToTic(multiScan, false);
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                Assert.That(multiScan.YArrays[i].SequenceEqual(yArrays[i]));
            }
        }

        [Test]
        public static void TestNormalizationTask()
        {
            // setup
            string filepath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"DataFiles\TDYeastFractionMS1.mzML");
            List<MsDataScan> scans = SpectraFileHandler.LoadMS1ScansFromFile(filepath);
            List<SingleScanDataObject> singleScans = SingleScanDataObject.ConvertMSDataScansInBulk(scans);
            NormalizationTask normalizationTask = new NormalizationTask();
            NormalizationOptions options = new NormalizationOptions() { PerformNormalization = false };

            // single scan data object
            SingleScanDataObject singleScan = singleScans.First();
            double[] yArray = singleScan.YArray;
            normalizationTask.RunSpecific(options, singleScan);
            Assert.That(singleScan.YArray.SequenceEqual(yArray));

            options.PerformNormalization = true;
            yArray = yArray.Select(p => p / singleScan.TotalIonCurrent).ToArray();
            normalizationTask.RunSpecific(options, singleScan);
            Assert.That(singleScan.YArray.SequenceEqual(yArray));

            // multi scan data object
            options.PerformNormalization = false;
            MultiScanDataObject multiScan = new MultiScanDataObject(singleScans.GetRange(1, 5));
            double[][] yArrays = multiScan.YArrays;
            normalizationTask.RunSpecific(options, multiScan);
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                Assert.That(multiScan.YArrays[i].SequenceEqual(yArrays[i]));
            }

            options.PerformNormalization = true;
            normalizationTask.RunSpecific(options, multiScan);
            for (int i = 0; i < multiScan.ScansToProcess; i++)
            {
                yArrays[i] = multiScan.YArrays[i].Select(p => p / multiScan.TotalIonCurrent[i]).ToArray();
                Assert.That(multiScan.YArrays[i].SequenceEqual(yArrays[i]));
            }
        }

        [Test]
        public static void TestNormalizationTestErrors()
        {
            // setup
            string filepath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"DataFiles\TDYeastFractionMS1.mzML");
            List<MsDataScan> scans = SpectraFileHandler.LoadMS1ScansFromFile(filepath);
            List<SingleScanDataObject> singleScans = SingleScanDataObject.ConvertMSDataScansInBulk(scans);
            NormalizationTask normalizationTask = new NormalizationTask();
            NormalizationOptions options = new NormalizationOptions() { PerformNormalization = false };
            StandardizationOptions standOptions = new StandardizationOptions();
            SingleScanDataObject singleScan = singleScans.First();

            // incorrect options
            try
            {
                normalizationTask.RunSpecific(standOptions, singleScan);
            }
            catch (ArgumentException e)
            {
                Assert.That(e.Message.Equals("Invalid options class for NormalizationTask"));
            }

            // null values
            try
            {
                normalizationTask.RunSpecific((NormalizationOptions)null, singleScan);
            }
            catch (ArgumentException e)
            {
                Assert.That(e.Message.Equals("Arguments cannot be null"));
            }
            try
            {
                normalizationTask.RunSpecific(options, singleScan = null);
            }
            catch (ArgumentException e)
            {
                Assert.That(e.Message.Equals("Arguments cannot be null"));
            }
        }
    }
}
