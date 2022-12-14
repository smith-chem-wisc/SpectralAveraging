using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using IO.MzML;
using MassSpectrometry;
using MathNet.Numerics.Distributions;
using SpectralAveraging;


namespace Tests
{
    public class TestScanCombination
    {
        private double[][] xArrays; 
        private double[][] yArrays;
        private double[] tics; 

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            #region Old, Easy test
            //// make ten arrays of the following: 
            //// make x array from 500 to 2000 m/z, spaced at 0.001 m/z apart
            //// make y array random 

            //double[][] xarrays = new double[10][];
            //double[][] yarrays = new double[10][];
            //double numberSteps = (2000 - 500) / 0.01;
            //double[] xarray = new double[(int)numberSteps];
            //for (int i = 0; i < numberSteps; i++)
            //{
            //    xarray[i] = 500 + i * 0.01;
            //}

            //// create random values with seed starting at 1551. 
            //int initialSeed = 1551; 

            //for (int i = 0; i < xarrays.GetLength(0); i++)
            //{
            //    xarrays[i] = xarray; 
            //    yarrays[i] = new double[(int)numberSteps];

            //    Random rnd = new Random(initialSeed + i);
            //    for (int j = 0; j < yarrays[i].Length; j++)
            //    {
            //        yarrays[i][j] = rnd.NextDouble() * 10000; 
            //    }
            //}
            //_xarrays = xarrays;
            //_yarrays = yarrays;
            //_tics = new double[xarrays.GetLength(0)];
            //for (int i = 0; i < _tics.Length; i++)
            //{
            //    _tics[i] = yarrays[i].Sum(); 
            //}
            #endregion

            // this tests more mass spectrometry-like data. 
            double stddev = 10d;
            double mean = 500; 
            double frontTerm = 1 / (stddev * Math.Sqrt(2 * Math.PI));

            Normal normalDist = new Normal(20, 1);
            // normal distribution to shift the m/z values by
            Normal mzShifts = new Normal(0, 0.01);

            xArrays = new double[10][];
            yArrays = new double[10][];
            tics = new double[10]; 
            for (int m = 0; m < xArrays.GetLength(0); m++)
            {
                double[] gaussian = Enumerable.Range(1, 1000)
                    .Select(i => 50 * frontTerm * Math.Exp(-0.5 * (i - mean) * (i - mean) / (stddev * stddev)))
                    .Select(i =>
                        100 * i + Normal.Sample(normalDist.RandomSource, normalDist.Mean, normalDist.StdDev))
                    .ToArray();
                double[] xAxis = Enumerable.Range(1, 1000)
                    .Select(i => i + Normal.Sample(mzShifts.RandomSource, mzShifts.Mean, mzShifts.StdDev))
                    .ToArray();

                xArrays[m] = new double[gaussian.Length]; 
                yArrays[m] = new double[gaussian.Length];

                yArrays[m] = gaussian;
                xArrays[m] = xAxis;
                tics[m] = gaussian.Sum(); 
            }
        }

        [Test]
        public void TestCombination()
        {
            using (StreamWriter sr = new("noisyOutput.txt"))
            {
                for (int i = 0; i < xArrays[0].Length; i++)
                {
                    sr.WriteLine("{0},{1}", xArrays[0][i], yArrays[0][i]);
                }
                sr.Flush();
            }

            Stopwatch sw = Stopwatch.StartNew();
            SpectralAveragingOptions options = new SpectralAveragingOptions();
            options.SetDefaultValues();
            options.BinSize = 1.0; 
            options.SpectrumMergingType = SpectrumMergingType.MrsNoiseEstimate;
            options.RejectionType = RejectionType.AveragedSigmaClipping; 
            double[][] results = SpectralMerging.CombineSpectra(xArrays, yArrays,
                tics, 3, options);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            using (StreamWriter sr = new("averagedOutputs.txt"))
            {
                for (int i = 0; i < results[0].Length; i++)
                {
                    sr.WriteLine("{0},{1}", results[0][i], results[1][i]);
                }
                sr.Flush();
            }
        }
    }
}
