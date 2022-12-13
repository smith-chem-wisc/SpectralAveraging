using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using Nett;
using SpectralAveraging.NoiseEstimates;

namespace SpectralAveraging.DataStructures
{

    public class BinnedSpectra
    {
        public List<PixelStack> PixelStacks { get; set; }
        // TODO: Concurrent dictionaries should be temporary, and these should be sorted Dictionaries. 
        public SortedDictionary<int, double> NoiseEstimates { get; private set; }
        public SortedDictionary<int, double> ScaleEstimates { get; private set; }
        public SortedDictionary<int, double> Weights { get; private set; }
        public double[] Tics { get; private set; }
        public int NumSpectra => PixelStacks[0].Intensity.Count;
        private List<double[]> RecalculatedSpectra { get; set; }

        public BinnedSpectra()
        {
            PixelStacks = new List<PixelStack>();
            NoiseEstimates = new SortedDictionary<int, double>();
            ScaleEstimates = new SortedDictionary<int, double>();
            Weights = new SortedDictionary<int, double>();
            RecalculatedSpectra = new List<double[]>(); 
        }

        public void ProcessPixelStacks(SpectralAveragingOptions options)
        {
            Parallel.ForEach(PixelStacks, pixelStack =>
            {
                pixelStack.PerformRejection(options);
            }); 
        }

        public void ConsumeSpectra(double[][] xArrays, double[][] yArrays,
            int numSpectra, double binSize)
        {
            double min = 100000;
            double max = 0;
            for (int i = 0; i < numSpectra; i++)
            {
                min = Math.Min(xArrays[i][0], min);
                max = Math.Max(xArrays[i].Max(), max);
            }

            int numberOfBins = (int)Math.Ceiling((max - min) * (1 / binSize));
            // go through each scan and place each (m/z, int) from the spectra into a jagged array

            // 1) find all values of x that fall within a bin.

            List<List<BinValue>> listBinValues = new();

            for (int i = 0; i < numSpectra; i++)
            {
               listBinValues.Add(CreateBinValueList(xArrays[i], yArrays[i], min, binSize));
               listBinValues[i].Sort((m, n) => m.Bin.CompareTo(n.Bin));
            }

            int[] spectraId = Enumerable.Range(0, numSpectra).ToArray(); 
            
            for (int i = 0; i < numberOfBins; i++)
            {
                List<double> xVals = new();
                List<double> yVals = new();
                foreach(var binValList in listBinValues)
                {
                    List<BinValue> binValRecord = new();
                    int index = binValList.BinarySearch(new BinValue() { Bin = i }, new BinValueComparer());
                    if (index < 0)
                    {
                        index = ~index;
                    }
                    int k = 0;
                    while (binValList[index + k].Bin == i && k + index < binValList.Count - 1)
                    {
                        binValRecord.Add(binValList[index + k]);
                        k++;
                        if (k + index > binValList.Count - 1)
                        {
                            break; 
                        }
                    }

                    if (binValRecord.Count() > 1)
                    {
                        xVals.Add(binValRecord.Average(m => m.Mz));
                        yVals.Add(binValRecord.Average(m => m.Intensity));
                    }
                    else
                    {
                        xVals.Add(binValRecord.First().Mz);
                        yVals.Add(binValRecord.First().Intensity);
                    }
                }

                PixelStacks.Add(new PixelStack(xVals, yVals, spectraId));
            }
            PixelStacks.Sort(new PixelStack.PixelStackComparer());
        }

        public void PerformNormalization()
        {
            Parallel.For(0, PixelStacks.Count, i =>
            {
                // pixelStacks[i].Length will be the number of spectra to be averaged, 
                // which is equal to tics. 
                for (int j = 0; j < PixelStacks[i].Length; j++)
                {
                    PixelStacks[i].Intensity[j] /= Tics[j];
                }
            }); 
        }

        public void CalculateNoiseEstimates(WaveletType waveletType = WaveletType.Haar, 
            double epsilon = 0.01, int maxIterations = 25)
        {
            if (!RecalculatedSpectra.Any())
            {
                for (int i = 0; i < NumSpectra; i++)
                {
                    double[] tempValArray = PopIntensityValuesFromPixelStackList(i);
                    bool success = MRSNoiseEstimator.MRSNoiseEstimation(tempValArray, epsilon, out double noiseEstimate,
                        waveletType: waveletType, maxIterations: maxIterations);
                    if (!success || double.IsNaN(noiseEstimate))
                    {
                        noiseEstimate = BasicStatistics.CalculateStandardDeviation(tempValArray);
                    }
                    NoiseEstimates.TryAdd(i, noiseEstimate);
                }
            }

            ConcurrentDictionary<int, double> tempConcurrentDictionary = new(); 
            RecalculatedSpectra
                .Select((w, i) => new { Index = i, Array = w })
                .AsParallel()
                .ForAll(x =>
                {
                    bool success = MRSNoiseEstimator.MRSNoiseEstimation(x.Array, epsilon, out double noiseEstimate,
                        waveletType: waveletType, maxIterations: maxIterations);
                    if (!success || double.IsNaN(noiseEstimate))
                    {
                        noiseEstimate = BasicStatistics.CalculateStandardDeviation(x.Array);
                    }
                    tempConcurrentDictionary.TryAdd(x.Index, noiseEstimate);
                });
            NoiseEstimates = new SortedDictionary<int, double>(tempConcurrentDictionary); 
        }

        public void CalculateScaleEstimates()
        {
            double reference = 0;
            if (!RecalculatedSpectra.Any())
            {
                for (int i = 0; i < NumSpectra; i++)
                {
                    double[] tempValArray = PopIntensityValuesFromPixelStackList(i);
                    double scale = BiweightMidvariance(tempValArray);
                    if (i == 0)
                    {
                        reference = scale;
                    }
                    ScaleEstimates.TryAdd(i, reference / scale);
                }

                return; 
            }
            ConcurrentDictionary<int, double> tempScaleEstimates = new();
            reference = BiweightMidvariance(RecalculatedSpectra[0]);
            tempScaleEstimates.TryAdd(0, reference / reference); 
            RecalculatedSpectra
                .Select((w,i) => new {Index = i, Array = w})
                .AsParallel().ForAll(x =>
                {
                    double scale = BiweightMidvariance(x.Array);
                    if (x.Index == 0)
                    {
                        return; 
                    }
                    tempScaleEstimates.TryAdd(x.Index, scale / reference);
                });
            ScaleEstimates = new SortedDictionary<int, double>(tempScaleEstimates); 
        }

        private double MedianAbsoluteDeviationFromMedian(double[] array)
        {
            double arrayMedian = BasicStatistics.CalculateMedian(array);
            double[] results = new double[array.Length];
            for (int j = 0; j < array.Length; j++)
            {
                results[j] = Math.Abs(array[j] - arrayMedian);
            }

            return BasicStatistics.CalculateMedian(results); 
        }

        private double BiweightMidvariance(double[] array)
        {
            double[] y_i = new double[array.Length];
            double[] a_i = new double[array.Length]; 
            double MAD_X = MedianAbsoluteDeviationFromMedian(array);
            double median = BasicStatistics.CalculateMedian(array); 
            for (int i = 0; i < y_i.Length; i++)
            {
                y_i[i] = (array[i] - median) / (9d * MAD_X);
                if (y_i[i] < 1d)
                {
                    a_i[i] = 1d; 
                }
                else
                {
                    a_i[i] = 0; 
                }
            }

            // biweight midvariance calculation

            double denomSum = 0;
            double numeratorSum = 0; 
            for (int i = 0; i < y_i.Length; i++)
            {
                numeratorSum += a_i[i] * Math.Pow(array[i] - median, 2) * Math.Pow(1 - y_i[i] * y_i[i], 4); 
                denomSum += a_i[i] * (1 - 5 * y_i[i] * y_i[i]) * (1 - y_i[i] * y_i[i]);
            }

            return (double)y_i.Length * numeratorSum / Math.Pow(Math.Abs(denomSum), 2); 
        }

        public void CalculateWeights()
        {
            foreach (var entry in NoiseEstimates)
            {
                var successScale = ScaleEstimates.TryGetValue(entry.Key,
                    out double scale);
                if (!successScale) continue;

                var successNoise = NoiseEstimates.TryGetValue(entry.Key,
                    out double noise);
                if (!successNoise) continue;

                double weight = 1d / Math.Pow((scale * noise), 2);

                Weights.TryAdd(entry.Key, weight);
            }
        }

        public void RecalculateTics()
        {
            Tics = new double[NumSpectra];
            RecalculatedSpectra = new List<double[]>(); 
            for (int i = 0; i < NumSpectra; i++)
            {
                RecalculatedSpectra.Add(PopIntensityValuesFromPixelStackList(i));
                Tics[i] = RecalculatedSpectra[i].Sum();
            }
        }

        public void MergeSpectra()
        {
            var weights = Weights.Values.ToArray(); 
            Parallel.ForEach(PixelStacks, pixelStack =>
            {
                pixelStack.Average(weights);
            });
        }

        public double[][] GetMergedSpectrum()
        {
            double[] xArray = PixelStacks.Select(i => i.Mz).ToArray();
            double[] yArray = PixelStacks.Select(i => i.MergedValue).ToArray();
            return new[] { xArray, yArray };
        }
        private static List<BinValue> CreateBinValueList(double[] xArray, double[] yArray,
            double min, double binSize)
        {
            var binIndices = xArray
                .Select((w, i) => 
                    new { Index = i, Bin = (int)Math.Floor((w - min) / binSize) }); 
            List<BinValue> binValues = new List<BinValue>();
            foreach (var bin in binIndices)
            {
                binValues.Add(new BinValue(bin.Bin, xArray[bin.Index], yArray[bin.Index]));
            }
            return binValues;
        }

        private double[] PopIntensityValuesFromPixelStackList(int index)
        {
            double[] results = new double[PixelStacks.Count];
            for (int i = 0; i < PixelStacks.Count; i++)
            {
                results[i] = PixelStacks[i].Intensity[index]; 
            }

            return results; 
        }
    }

    /// <summary>
    /// Record type used to facilitate bin, mz, and intensity matching. 
    /// </summary>
    /// <param name="Bin"></param>
    /// <param name="Mz"></param>
    /// <param name="Intensity"></param>
    internal readonly record struct BinValue(int Bin, double Mz, double Intensity);

    internal class BinValueComparer : IComparer<BinValue>
    {
        public int Compare(BinValue x, BinValue y)
        {
            return x.Bin.CompareTo(y.Bin); 
        }
    }
}
