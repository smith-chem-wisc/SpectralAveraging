
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic.FileIO;
using SpectralAveraging;
using SpectralAveraging.NoiseEstimates;



namespace Tests
{

    public class NoiseEstimatorTests
    {

        [Test]
        public void TestModWt()
        {
            var signal = Enumerable.Range(1, 1024)
                .Select(i => Math.Sin(i / 60d)).ToArray();

            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            var modwtResult = WaveletMath.ModWt(signal, wflt);
            modwtResult.PrintToTxt(Path.Combine(TestContext.CurrentContext.WorkDirectory, "modwtOutput.txt"));
        }

        [Test]
        public void TestCreateWavelet()
        {
            var wflt = new WaveletFilter(); 
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            double[] expectedValWavelets = { 0.500, 0.500 };
            double[] expectedScaling = { 0.5d, -0.5 };
            Assert.That(expectedValWavelets, Is.EqualTo(wflt.WaveletCoefficients).Within(0.01));
            Assert.That(expectedScaling, Is.EqualTo(wflt.ScalingCoefficients).Within(0.01));
        }

        [Test]
        public void TestCreateReflectedArray()
        {
            double[] testArray = { 0, 1, 2, 3, 4, 5 };
            double[] result = WaveletMath.CreateReflectedArray(testArray);
            double[] expected = { 0, 1, 2, 3, 4, 5, 5, 4, 3, 2, 1, 0 };
            Assert.That(result.Length, Is.EqualTo(6*2));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"C:\Users\Austin\Desktop\ubiquitin_noise20.csv")]
        public void TestCreateMultiResolutionSupport(string path)
        {
            List<double> mzVals = new();
            List<double> intensityVals = new(); 
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = false;
                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    mzVals.Add(Convert.ToDouble(fields[0]));
                    intensityVals.Add(Convert.ToDouble(fields[1]));
                }
            }
            
            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            double[] signal = intensityVals.ToArray(); 
            var modwtResult = WaveletMath.ModWt(signal, wflt);

            double stdev = BasicStatistics.CalculateStandardDeviation(signal);
            

        }

        [Test]
        [TestCase(@"C:\Users\Austin\Desktop\ubiquitin_noise20.csv")]
        public void TestNoiseStdEstimated(string path)
        {
            List<double> mzVals = new();
            List<double> intensityVals = new();
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = false;
                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    mzVals.Add(Convert.ToDouble(fields[0]));
                    intensityVals.Add(Convert.ToDouble(fields[1]));
                }
            }

            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            double[] signal = intensityVals.ToArray();
            bool noiseEstimationSuccess = MRSNoiseEstimator.MRSNoiseEstimation(signal, 0.1, out double noiseEstimate);
            Assert.That(noiseEstimate, Is.EqualTo(29.2).Within(0.1));
            Assert.True(noiseEstimationSuccess);
        }

        [Test]
        [TestCase(@"C:\Users\Austin\Desktop\ubiquitin_noise20.csv")]
        public void TestSumWavelet(string path)
        {
            List<double> mzVals = new();
            List<double> intensityVals = new();
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = false;
                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    mzVals.Add(Convert.ToDouble(fields[0]));
                    intensityVals.Add(Convert.ToDouble(fields[1]));
                }
            }
            double[] signal = intensityVals.ToArray();


            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            var modwtResult = WaveletMath.ModWt(signal, wflt);

            var summedWaveletCoeff = modwtResult.SumWaveletCoefficients();
            var waveletCoeffs = modwtResult.Levels.Select(i => i.WaveletCoeff).ToList();
            using (StreamWriter wr =
                   new(Path.Combine(TestContext.CurrentContext.WorkDirectory, "testSummedOutput.csv")))
            {
                foreach (double[] array in waveletCoeffs)
                {
                    wr.WriteLine(string.Join(",", array));
                }
                
            }
            
            // add actual test data

        }
        
    }

}
