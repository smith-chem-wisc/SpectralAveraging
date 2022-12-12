using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.MzML;
using MassSpectrometry;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NUnit.Framework.Interfaces;
using SpectralAveraging;
using SpectralAveraging.DataStructures;

namespace Tests;

public class TestBinnedSpectra
{
    private double[][] xArrays;
    private double[][] yArrays;
    private double[] tics;
    private int numSpectra;
    private double binSize; 

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        xArrays = new[]
        {
            new double[] { 0, 1, 2, 3, 3.5, 4 },
            new double[] { 0, 1, 2, 3, 4 },
            new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
        };
        yArrays = new[]
        {
            new double[] { 10, 11, 12, 12, 13, 14 },
            new double[] { 11, 12, 13, 14, 15 },
            new double[] { 20, 25, 30, 35, 40 }
        };
        tics = new double[3];
        tics[0] = yArrays[0].Sum();
        tics[1] = yArrays[1].Sum();
        tics[2] = yArrays[2].Sum();
        numSpectra = 3;
        binSize = 1.0; 
    }


    [Test]
    public void TestConsumeSpectra()
    {
        BinnedSpectra binnedSpectra = new();

        int numSpectra = 3;
        double binSize = 1.0;

        binnedSpectra.ConsumeSpectra(xArrays, yArrays, tics, numSpectra, binSize); 
        Assert.True(binnedSpectra.PixelStacks.Count == xArrays[0].Length);
        Assert.True(binnedSpectra.PixelStacks[0].Mz == 0.1d);
        Assert.That(binnedSpectra.PixelStacks[2].Intensity, 
            Is.EqualTo(new double[] {12, 13, 30}));
    }
    [Test]
    public void TestConsumeSpectraUnequalArrayLength()
    {
        BinnedSpectra binnedSpectra = new();
        int numSpectra = 3;
        double binSize = 1.0;
        binnedSpectra.ConsumeSpectra(xArrays, yArrays, tics, numSpectra, binSize);
        Assert.True(binnedSpectra.PixelStacks.Count == 5);
        // 12.5 is the correct answer if the binning and averaging is correct. 
        // Value will be exactly 12.5, so okay to use 12.5 explicitly.  
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        Assert.True(binnedSpectra.PixelStacks[3].Intensity[0] == 12.5d);
    }

    [Test]
    public void TestPerformNormalization()
    {
        BinnedSpectra bs = new();
        bs.ConsumeSpectra(xArrays, yArrays, tics, numSpectra, binSize);
        bs.PerformNormalization(tics);
        double[] expected = new double[]
        {
            yArrays[0][0] / tics[0], 
            yArrays[1][0] / tics[1], 
            yArrays[2][0] / tics[2], 

        };
        Assert.That(bs.PixelStacks[0].Intensity.ToArray(), 
            Is.EqualTo(expected).Within(0.01));
    }

    [Test]
    public void TestCalculateNoiseEstimate()
    {
        // This is just testing to see if the noise estimates work.
        // The values produces by the NoiseEstimation are tested elsewhere. 
        BinnedSpectra bs = new();
        bs.ConsumeSpectra(xArrays, yArrays, tics, numSpectra, binSize);
        bs.CalculateNoiseEstimates();
        foreach (var estimate in bs.NoiseEstimates)
        {
            Console.WriteLine("Spectra {0}. Noise estimate: {1}", 
                estimate.Key, estimate.Value);
        }
    }

}
