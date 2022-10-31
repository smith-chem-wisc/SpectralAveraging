using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using SpectralAveraging;
namespace Tests
{
    internal class TestWeighting
    {
        [Test]
        public static void TestWeightedAverage()
        {
            double[] values = new double[] { 10, 0 };
            double[] weights = new double[] { 8, 2 };
            double average = SpectralMerging.MergePeakValuesToAverage(values, weights);
            Assert.That(average, Is.EqualTo(8));

            values = new double[] { 10, 2, 0 };
            weights = new double[] { 9, 1, 0 };
            average = SpectralMerging.MergePeakValuesToAverage(values, weights);
            Assert.That(Math.Round(average, 4), Is.EqualTo(9.200));
        }

        [Test]
        public static void TestCalculatingWeights()
        {
            double[] test = new double[] { 10, 8, 6, 5, 4, 3, 2, 1 };
            double[] weights = new double[test.Length];
            BinWeighting.WeightByNormalDistribution(test, ref weights);
            double weightedAverage = SpectralMerging.MergePeakValuesToAverage(test, weights);
            Assert.That(Math.Round(weightedAverage, 4), Is.EqualTo(4.5460));

            weights = new double[test.Length];
            BinWeighting.WeightByCauchyDistribution(test, ref weights);
            weightedAverage = SpectralMerging.MergePeakValuesToAverage(test, weights);
            Assert.That(Math.Round(weightedAverage, 4), Is.EqualTo(4.6411));

            weights = new double[test.Length];
            BinWeighting.WeightByPoissonDistribution(test, ref weights);
            weightedAverage = SpectralMerging.MergePeakValuesToAverage(test, weights);
            Assert.That(Math.Round(weightedAverage, 4), Is.EqualTo(5.0244));

            weights = new double[test.Length];
            BinWeighting.WeightByGammaDistribution(test, ref weights);
            weightedAverage = SpectralMerging.MergePeakValuesToAverage(test, weights);
            Assert.That(Math.Round(weightedAverage, 4), Is.EqualTo(4.7196));
        }
	}
}
