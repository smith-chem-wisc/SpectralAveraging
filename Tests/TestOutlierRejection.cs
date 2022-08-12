
using SpectralAveraging;

namespace Tests
{
    public static class TestOutlierRejection
    {
        [Test]
		public static void TestClippingFunctions()
        {
			#region Min Max Clipping

			double[] test = { 10, 9, 8, 7, 6, 5 };
			double[] expected = { 9, 8, 7, 6 };
			double[] minMaxClipped = OutlierRejection.MinMaxClipping(test);
			Assert.That(minMaxClipped, Is.EqualTo(expected));

			#endregion

			#region Percentile Clipping

			test = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			expected = new double[] { 90, 80, 70, 60, 50, 40, 30, 20, 10 };
			double[] percentileClipped = OutlierRejection.PercentileClipping(test, 0.9);
			Assert.That(percentileClipped, Is.EqualTo(expected));

			test = new double[] { 100, 80, 60, 50, 40, 30, 20, 10, 0 };
			expected = new double[] { 60, 50, 40, 30, 20, 10 };
			percentileClipped = OutlierRejection.PercentileClipping(test, 0.9);
			Assert.That(percentileClipped, Is.EqualTo(expected));

			#endregion

			#region Sigma Clipping

			test = new double[] { 100, 80, 60, 50, 40, 30, 20, 10, 0 };
			double[] sigmaClipped = OutlierRejection.SigmaClipping(test, 1.5, 1.5);
			expected = new double[] { 50, 40, 30, 20, 10 };
			Assert.That(sigmaClipped, Is.EqualTo(expected));

			test = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			sigmaClipped = OutlierRejection.SigmaClipping(test, 1, 1);
			expected = new double[] { 50 };
			Assert.That(sigmaClipped, Is.EqualTo(expected));

			sigmaClipped = OutlierRejection.SigmaClipping(test, 1.3, 1.3);
			expected = new double[] { 60, 50, 40 };
			Assert.That(sigmaClipped, Is.EqualTo(expected));

			sigmaClipped = OutlierRejection.SigmaClipping(test, 1.5, 1.5);
			expected = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			Assert.That(sigmaClipped, Is.EqualTo(expected));

			#endregion

			#region Windsorized Sigma Clipping

			test = new double[] { 100, 80, 60, 50, 40, 30, 20, 10, 0 };
			double[] windsorizedSigmaClipped = OutlierRejection.WinsorizedSigmaClipping(test, 1.5, 1.5);
			expected = new double[] { 60, 50, 40, 30, 20, 10, 0 };
			Assert.That(windsorizedSigmaClipped, Is.EqualTo(expected));

			test = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			windsorizedSigmaClipped = OutlierRejection.WinsorizedSigmaClipping(test, 1, 1);
			expected = new double[] { 60, 50, 40 };
			Assert.That(windsorizedSigmaClipped, Is.EqualTo(expected));

			windsorizedSigmaClipped = OutlierRejection.WinsorizedSigmaClipping(test, 1.3, 1.3);
			expected = new double[] { 60, 50, 40 };
			Assert.That(windsorizedSigmaClipped, Is.EqualTo(expected));

			windsorizedSigmaClipped = OutlierRejection.WinsorizedSigmaClipping(test, 1.5, 1.5);
			expected = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			Assert.That(windsorizedSigmaClipped, Is.EqualTo(expected));

			#endregion

			#region Averaged Sigma Clipping

			test = new double[] { 100, 80, 60, 50, 40, 30, 20, 10, 0 };
			double[] averagedSigmaClipping = OutlierRejection.AveragedSigmaClipping(test, 3, 3);
			expected = new double[] { 80, 60, 50, 40, 30, 20, 10, 0 };
			Assert.That(averagedSigmaClipping, Is.EqualTo(expected));

			test = new double[] { 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 0 };
			averagedSigmaClipping = OutlierRejection.AveragedSigmaClipping(test, 1, 1);
			expected = new double[] { 70, 60, 50, 40, 30, };
			Assert.That(averagedSigmaClipping, Is.EqualTo(expected));

			averagedSigmaClipping = OutlierRejection.AveragedSigmaClipping(test, 1.3, 1.3);
			expected = new double[] { 80, 70, 60, 50, 40, 30, 20 };
			Assert.That(averagedSigmaClipping, Is.EqualTo(expected));

			averagedSigmaClipping = OutlierRejection.AveragedSigmaClipping(test, 1.5, 1.5);
			expected = new double[] { 80, 70, 60, 50, 40, 30, 20 };
			Assert.That(averagedSigmaClipping, Is.EqualTo(expected));

			#endregion

		}


		[Test]
		public static void TestSetValuesAndRejectOutliersSwitch()
		{
            SpectralAveragingOptions options = new SpectralAveragingOptions();

			options.SetDefaultValues();
			Assert.That(options.RejectionType == RejectionType.NoRejection);
			Assert.That(options.WeightingType == WeightingType.NoWeight);
			Assert.That(0.9, Is.EqualTo(options.Percentile));
			Assert.That(1.3, Is.EqualTo(options.MinSigmaValue));
			Assert.That(1.3, Is.EqualTo(options.MaxSigmaValue));

			options.SetValues(RejectionType.MinMaxClipping, WeightingType.NoWeight, SpectrumMergingType.SpectrumBinning, true, .8, 2, 4);
			Assert.That(options.RejectionType == RejectionType.MinMaxClipping);
			Assert.That(0.8, Is.EqualTo(options.Percentile));
			Assert.That(2, Is.EqualTo(options.MinSigmaValue));
			Assert.That(4, Is.EqualTo(options.MaxSigmaValue));

			options.SetDefaultValues();
			double[] test = new double[] { 10, 8, 6, 5, 4, 2, 0 };
			double[] output = OutlierRejection.RejectOutliers(test, options);
			double[] expected = new double[] { 10, 8, 6, 5, 4, 2, 0 };
			Assert.That(output, Is.EqualTo(expected));

			options.SetValues(rejectionType: RejectionType.MinMaxClipping);
			Assert.That(options.RejectionType == RejectionType.MinMaxClipping);
			output = OutlierRejection.RejectOutliers(test, options);
			expected = new double[] { 8, 6, 5, 4, 2 };
			Assert.That(output, Is.EqualTo(expected));

			options.SetValues(rejectionType: RejectionType.PercentileClipping);
			Assert.That(options.RejectionType == RejectionType.PercentileClipping);
			options.SetValues(rejectionType: RejectionType.SigmaClipping);
			Assert.That(options.RejectionType == RejectionType.SigmaClipping);
			options.SetValues(rejectionType: RejectionType.WinsorizedSigmaClipping);
			Assert.That(options.RejectionType == RejectionType.WinsorizedSigmaClipping);
			options.SetValues(rejectionType: RejectionType.AveragedSigmaClipping);
			Assert.That(options.RejectionType == RejectionType.AveragedSigmaClipping);
		}

		

  //      [Test]
		//public static void TestSpectrumAveragingErrors()
  //      {
		//	string filepath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"DataFiles\TDYeastFractionMS1.mzML");
		//	List<MsDataScan> scans = SpectraFileHandler.LoadMS1ScansFromFile(filepath);
		//	List<MsDataScan> reducedScans = scans.GetRange(400, 5);
		//	List<SingleScanDataObject> singleScanDataObjects = SingleScanDataObject.ConvertMSDataScansInBulk(reducedScans);
		//	MultiScanDataObject multiScanDataObject = new MultiScanDataObject(singleScanDataObjects);
		//	SpectrumAveragingTask averagingTask = new();
		//	SpectrumAveragingOptions options = new SpectrumAveragingOptions();
		//	options.SetDefaultValues();

		//	// data error
		//	try
		//	{
		//		averagingTask.RunSpecific(options, singleScanDataObjects.First());
		//		Assert.Fail();
		//	}
		//	catch (ArgumentException e)
		//	{
		//		Assert.That(e.Message.Equals("Invalid data class for this class and method."));
		//	}

		//	// options error
		//	try
		//	{
		//		averagingTask.RunSpecific(new Normalization.NormalizationOptions(), multiScanDataObject);
		//		Assert.Fail();
		//	}
		//	catch (ArgumentException e)
		//	{
		//		Assert.That(e.Message.Equals("Invalid options class for this class and method."));
		//	}

		//	// null errors
		//	try
  //          {
		//		averagingTask.RunSpecific((SpectrumAveragingOptions)null, multiScanDataObject);
		//		Assert.Fail();
  //          }
		//	catch (ArgumentException e)
  //          {
		//		Assert.That(e.Message.Equals("Data or options passed to method is null."));
  //          }
  //          try
  //          {
		//		averagingTask.RunSpecific(options, multiScanDataObject = null);
		//		Assert.Fail();
  //          }
		//	catch (ArgumentException e)
  //          {
		//		Assert.That(e.Message.Equals("Data or options passed to method is null."));
  //          }
		//}
	}

}
