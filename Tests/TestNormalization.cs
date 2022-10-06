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
    }
}
