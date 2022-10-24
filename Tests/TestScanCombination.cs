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
    public class TestScanCombination
    {
        private double[][] _xarrays; 
        private double[][] _yarrays;
        private double[] _tics; 

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // make ten arrays of the following: 
            // make x array from 500 to 2000 m/z, spaced at 0.001 m/z apart
            // make y array random 

            double[][] xarrays = new double[1000][];
            double[][] yarrays = new double[1000][];
            double numberSteps = (2000 - 500) / 0.001;
            double[] xarray = new double[(int)numberSteps];
            for (int i = 0; i < numberSteps; i++)
            {
                xarray[i] = 500 + i * 0.001;
            }

            // create random values with seed starting at 1551. 
            int initialSeed = 1551; 

            for (int i = 0; i < 1000; i++)
            {
                xarrays[i] = xarray; 
                yarrays[i] = new double[(int)numberSteps];

                Random rnd = new Random(initialSeed + i);
                for (int j = 0; j < yarrays[i].Length; j++)
                {
                    yarrays[i][j] = rnd.NextDouble() * 10000; 
                }
            }
            _xarrays = xarrays;
            _yarrays = yarrays;
            _tics = new double[1000];
            for (int i = 0; i < _tics.Length; i++)
            {
                _tics[i] = yarrays[i].Sum(); 
            }
        }

        [Test]
        public void TestCombination()
        {
            SpectralAveragingOptions options = new SpectralAveragingOptions();
            options.SetDefaultValues();
            double[][] results = SpectralMerging.CombineSpectra(_xarrays, _yarrays, _tics, 1000, options);
        }
    }
}
