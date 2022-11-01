using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using IO.MzML;
using MassSpectrometry;
using SpectralAveraging;


namespace Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestNormalization
    {
        [Test]
        public static void TestBaseNormalizationFunctions()
        {
            double[] sampleData = new double[] { 100, 80, 70, 60, 50, 40 };
            double[] expected = new double[] { 0.25, 0.2, 0.175, 0.15, 0.125, 0.1 };
            double[] badSampleData = new double[] { 0, 0, 0, 0, 0 };
            SpectrumNormalization.NormalizeSpectrumToTic(ref sampleData, 400);
            Assert.That(Math.Abs(sampleData.Sum() - 1) < 0.001);
            Assert.That(sampleData.SequenceEqual(expected));

            SpectrumNormalization.NormalizeSpectrumToTic(ref badSampleData, 400);
            Assert.That(badSampleData.All(p => p == 0));

            sampleData = new double[] { 100, 80, 70, 60, 50, 40 };
            SpectrumNormalization.NormalizeSpectrumToTic(sampleData, 400);
            Assert.That(Math.Abs(sampleData.Sum() - 1) < 0.001);
            Assert.That(sampleData.SequenceEqual(expected));

            SpectrumNormalization.NormalizeSpectrumToTic(badSampleData, 400);
            Assert.That(badSampleData.All(p => p == 0));
        }

        [Test]
        public static void TestAvgTicNormalizedSpectrum()
        {
            double[] sampleData = new double[] { 100, 80, 70, 60, 50, 40 };
            double[] expected = new double[] { 25, 20, 17.5, 15, 12.5, 10 };
            double[] badSampleData = new double[] { 0, 0, 0, 0, 0 };
            SpectrumNormalization.NormalizeSpectrumToTic(sampleData, 400, 100);
            Assert.That(Math.Abs(sampleData.Sum() - 100) < 0.001);
            Assert.That(sampleData.SequenceEqual(expected));

            SpectrumNormalization.NormalizeSpectrumToTic(badSampleData, 400, 100);
            Assert.That(badSampleData.All(p => p == 0));
        }

        
    }
}
