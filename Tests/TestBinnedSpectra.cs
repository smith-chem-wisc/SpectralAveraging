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
    [Test]
    public void TestConsumeSpectra()
    {
        BinnedSpectra binnedSpectra = new();

        double[][] xarrays = new[]
        {
            new double[] { 0, 1, 2, 3, 4 },
            new double[] { 0, 1, 2, 3, 4 },
            new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
        };
        double[][] yarrays = new[]
        {
            new double[] { 10, 11, 12, 13, 14 },
            new double[] { 11, 12, 13, 14, 15 },
            new double[] { 20, 25, 30, 35, 40 }
        };
        double[] tics = new double[3];
        tics[0] = yarrays[0].Sum();
        tics[1] = yarrays[1].Sum();
        tics[2] = yarrays[2].Sum();

        int numSpectra = 3;
        double binSize = 1.0;

        binnedSpectra.ConsumeSpectra(xarrays, yarrays, tics, numSpectra, binSize); 
        Assert.True(binnedSpectra.PixelStacks.Count == xarrays[0].Length);
        Assert.True(binnedSpectra.PixelStacks[0].Mz == 0.1d);
        Assert.That(binnedSpectra.PixelStacks[2].Intensity, 
            Is.EqualTo(new double[] {12, 13, 30}));
    }
    [Test]
    public void TestConsumeSpectraUnequalArrayLength()
    {
        BinnedSpectra binnedSpectra = new();

        double[][] xarrays = new[]
        {
            new double[] { 0, 1, 2, 3, 3.5, 4 },
            new double[] { 0, 1, 2, 3, 4 },
            new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
        };
        double[][] yarrays = new[]
        {
            new double[] { 10, 11, 12, 12, 13, 14 },
            new double[] { 11, 12, 13, 14, 15 },
            new double[] { 20, 25, 30, 35, 40 }
        };
        double[] tics = new double[3];
        tics[0] = yarrays[0].Sum();
        tics[1] = yarrays[1].Sum();
        tics[2] = yarrays[2].Sum();

        int numSpectra = 3;
        double binSize = 1.0;

        binnedSpectra.ConsumeSpectra(xarrays, yarrays, tics, numSpectra, binSize);
        Assert.True(binnedSpectra.PixelStacks.Count == xarrays[0].Length);
        Assert.True(binnedSpectra.PixelStacks[0].Mz == 0.1d);
        Assert.That(binnedSpectra.PixelStacks[2].Intensity,
            Is.EqualTo(new double[] { 12, 13, 30 }));
    }
}