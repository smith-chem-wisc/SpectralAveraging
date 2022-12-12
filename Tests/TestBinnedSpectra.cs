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

        double[][] xArrays = new[]
        {
            new double[] { 0, 1, 2, 3, 3.5, 4 },
            new double[] { 0, 1, 2, 3, 4 },
            new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
        };
        double[][] yArrays = new[]
        {
            new double[] { 10, 11, 12, 12, 13, 14 },
            new double[] { 11, 12, 13, 14, 15 },
            new double[] { 20, 25, 30, 35, 40 }
        };
        double[] tics = new double[3];
        tics[0] = yArrays[0].Sum();
        tics[1] = yArrays[1].Sum();
        tics[2] = yArrays[2].Sum();

        int numSpectra = 3;
        double binSize = 1.0;



        double min = 100000;
        double max = 0;
        for (int i = 0; i < numSpectra; i++)
        {
            min = Math.Min(xArrays[i][0], min);
            max = Math.Max(xArrays[i].Max(), max);
        }

        int numberOfBins = (int)Math.Ceiling((max - min) * (1 / binSize));

        double[][] xValuesBin = new double[numberOfBins][];
        double[][] yValuesBin = new double[numberOfBins][];
        // go through each scan and place each (m/z, int) from the spectra into a jagged array

        // 1) find all values of x that fall within a bin.

        // 2) perform a cubic spline interpolation to find the value of the 
        // central point (aka the bin center). 

        for (int i = 0; i < numSpectra; i++)
        {
            // currently this updates the y value with the most recent value from the array. 
            // what it really needs to do is a linear interpolation. 

            var binIndices = xArrays[i]
                .Select(i => (int)Math.Floor((i - min) / binSize))
                .ToArray();
            for (int j = 0; j < xValuesBin.Length; j++)
            {
                if (xValuesBin[j] == null)
                {
                    xValuesBin[j] = new double[numSpectra];
                    yValuesBin[j] = new double[numSpectra]; 
                }

                // Ideally, each value from the array falls into one and only one 
                // bin. However, this is going to rarely be the case.

                // If there are two array values that need to go to the third index, 
                // what happens? There should be some interpolation that occurs to select 
                // a "true" x value. Then you should also do the same with the y value. 

                // find the number of x array values that fall in the current bin: 
                var subsetBin;
                

                int countInBin = subsetBinIndices.Count(); 
                if (countInBin > 1)
                {
                    if (countInBin == 2)
                    {
                        // calculate central bin value. 
                        double centralBinValue = subsetBinIndices.Average(); 
                        // linear interpolation


                    }

                    if (countInBin > 3)
                    {
                        // some sort of non-linear interpolation
                    }
                }




                //if (xValuesBin[binIndex] == null)
                //{
                //    xValuesBin[binIndex] = new double[numSpectra];
                //    yValuesBin[binIndex] = new double[numSpectra];
                //}

                //xValuesBin[binIndex][i] = xArrays[i][j];
                //yValuesBin[binIndex][i] = yArrays[i][j];
            }
        }
    }

    private static double LinearInterpolation(double )
    {

    }
}