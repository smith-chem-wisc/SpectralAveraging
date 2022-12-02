
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Distributions;
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
        [TestCase(WaveletType.Haar, 
            new double[]{0.5d, 0.5d}, 
            new double[] {0.5d,-0.5d})]
        [TestCase(WaveletType.Db4, new[] {
                0.1629,    
                0.5055, 
                0.4461,
                -0.0198,
                -0.1323,
                0.0218,
                0.0233,
                -0.0075
            }, 
            new[] 
            {
                -0.0075,
                -0.0233,
                0.0218,
                0.1323,
                -0.0198, 
                -0.4461,
                0.5055,  
                -0.1629

            }
            )]
        public void TestCreateWavelet(WaveletType type, 
            double[] expectedScaling, 
            double[] expectedWavelet)
        {
            var wflt = new WaveletFilter(); 
            wflt.CreateFiltersFromCoeffs(type);
            Assert.That(wflt.WaveletCoefficients, Is.EqualTo(expectedWavelet).Within(0.01));
            Assert.That(wflt.ScalingCoefficients, Is.EqualTo(expectedScaling).Within(0.01));
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
            double stddevSignal = BasicStatistics.CalculateStandardDeviation(signal);
            bool noiseEstimationSuccess = MRSNoiseEstimator.MRSNoiseEstimation(signal, 0.1, out double noiseEstimate);
            
            Assert.That(noiseEstimate, Is.EqualTo(29.2).Within(0.1));
            Assert.True(noiseEstimationSuccess);
        }

        [Test]
        [TestCase(@"C:\Users\Austin\Desktop\ubiquitin_noise20.csv")]
        public void TestDb4WaveletNoiseEstimation(string path)
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
            wflt.CreateFiltersFromCoeffs(WaveletType.Db4);
            double[] signal = intensityVals.ToArray();
            bool noiseEstimationSuccess = MRSNoiseEstimator.MRSNoiseEstimation(signal, 0.1, out double noiseEstimate);
            //Assert.That(noiseEstimate, Is.EqualTo(29.2).Within(0.1));
            //Assert.True(noiseEstimationSuccess);
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

        [Test]
        [TestCase(0d, 0.5)]
        [TestCase(1d, 0.75d)]
        [TestCase(2d, 0.5d)]
        public void TestSimulatedNoiseSineWave(double mean, double stdev)
        {
            Normal normalDist = new Normal(mean, stdev);
            
            // generate sine wave
            double[] sineWave = Enumerable.Range(0, 1000)
                .Select(i => Math.Sin((double)i/(2*Math.PI)))
                .Select(i => 
                    5d*i + Normal.Sample(normalDist.RandomSource, normalDist.Mean, normalDist.StdDev))
                .ToArray();
            double noiseEstimate = BasicStatistics.CalculateStandardDeviation(sineWave.ToList());
            MRSNoiseEstimator.MRSNoiseEstimation(sineWave, epsilon: 0.1, 
                out double mrsEstimate, 
                waveletType:WaveletType.Db4);
            Console.WriteLine(mrsEstimate); 
        }

        [Test]
        [TestCase(500d, 50)]
        public void TestSimulatedNoiseGaussianPeak(double mean, double stddev)
        {
            double frontTerm = 1 / (stddev * Math.Sqrt(2 * Math.PI));
            Normal normalDist = new Normal(20, 5);
            double[] gaussian = Enumerable.Range(0, 1000)
                .Select(i => 50*frontTerm * Math.Exp(-0.5 * (i - mean) * (i - mean) / (stddev * stddev)))
                .Select(i =>
                    100*i + Normal.Sample(normalDist.RandomSource, normalDist.Mean, normalDist.StdDev))
                .ToArray();
            double noiseEstimateStddev = BasicStatistics.CalculateStandardDeviation(gaussian);
            MRSNoiseEstimator.MRSNoiseEstimation(gaussian, epsilon: 0.001,
                out double mrsNoiseEstimate,
                waveletType:WaveletType.Haar);
            Console.WriteLine("Stddev: {0}\nMrs: {1}", noiseEstimateStddev, mrsNoiseEstimate); 

        }

        [Test]
        [TestCase(WaveletType.Haar)]
        [TestCase(WaveletType.Db4)]
        public void TestRealData(WaveletType waveType)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles",
                "RealScanCarbonicAnhydrase.csv");  
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
            wflt.CreateFiltersFromCoeffs(WaveletType.Db4);
            double[] signal = intensityVals.ToArray();
            double noiseStddev = BasicStatistics.CalculateStandardDeviation(signal); 
            bool noiseEstimationSuccess = MRSNoiseEstimator.MRSNoiseEstimation(signal,
                0.1, out double noiseEstimate, waveletType:waveType);
            Console.WriteLine("Standard Deviation: {0}; MRS: {1}", noiseStddev, noiseEstimate);
        }
    }

}
