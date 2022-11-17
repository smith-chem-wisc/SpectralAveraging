
using System.Security.Cryptography.X509Certificates;
using SpectralAveraging;
using SpectralAveraging.NoiseEstimates;


namespace Tests
{

    public class NoiseEstimatorTests
    {
        [Test]
        public void TestClosestPow2()
        {
            int val1 = 17;
            int val2 = 3;
            int val3 = 64;

            int res1 = NoiseEstimators.ClosestPow2(val1); 
            int res2 = NoiseEstimators.ClosestPow2(val2); 
            int res3 = NoiseEstimators.ClosestPow2(val3);

            Assert.That(res1, Is.EqualTo(32)); 
            Assert.That(res2, Is.EqualTo(4));
            Assert.That(res3, Is.EqualTo(64));
        }

        [Test]
        public void TestPadZeroes()
        {
            double[] testArray = { 1d, 2d, 3d };
            double[] expected = { 1d, 2d, 3d, 0d }; 
            NoiseEstimators.PadZeroes(testArray, out double[] paddedSignal);
            Assert.That(paddedSignal, Is.EqualTo(expected));
        }

        [Test]
        public void TestModWt()
        {
            var signal = Enumerable.Range(1, 1024)
                .Select(i => Math.Sin(i / 60d)).ToArray();

            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            var modwtResult = WaveletMath.ModWt(signal, wflt);

            double[] expectedScaling =
            {
                -0.488796241,
                0.008332948,
                0.024996528,
                0.041653165,
                0.058298232,
                0.074927106,
            };
            Assert.That(modwtResult.Levels[0].ScalingCoeff[0..6], Is.EqualTo(expectedScaling).Within(0.1));
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
        public void TestSumWaveletCoefficients()
        {
            var signal = Enumerable.Range(0, 1024)
                .Select(i => Math.Sin(i / 60d)).ToArray();

            WaveletFilter wflt = new WaveletFilter();
            wflt.CreateFiltersFromCoeffs(WaveletType.Haar);
            var modwtResult = WaveletMath.ModWt(signal, wflt);

            double[] results = modwtResult.SumWaveletCoefficients(); 
        }
    }

}
