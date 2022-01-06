using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    partial class Program
    {


        private static void Datasampler(ref DemodulatorContext currentdemodulator, TestSignalInfo testsignal, ref StringBuilder console_output)
        {

            double[] zero_correlation = currentdemodulator.CorrelatorParameters.ZeroDemodulatorResult;
            double[] one_correlation = currentdemodulator.CorrelatorParameters.OneDemodulatorResult;
            double decimated_sampleperiod = currentdemodulator.DecimatedSamplePeriod;

            currentdemodulator.DataSlicerResults.OnePeaks = new double[59];
            currentdemodulator.DataSlicerResults.ZeroPeaks = new double[59];
            currentdemodulator.DataSlicerResults.OneWeightedPeaks = new double[59];
            currentdemodulator.DataSlicerResults.ZeroWeightedPeaks = new double[59];
            currentdemodulator.DataSlicerResults.SecondSampleRatios = new double[59];
            currentdemodulator.DataSlicerResults.SecondSampleTimes = new double[59];
            currentdemodulator.DataSlicerResults.RatioVsThreshold = new double[59];
            currentdemodulator.DemodulationResult.DemodulatedData = new bool[59];

            double datasampler_bias_scale_offset = 0;
            // offset to the ratio of one/zero
            double datasampler_ratio_offset = 0;
            int datasampler_start = 0;
            int datasampler_stop = 0;

            double datasampler_threshold = 1;
            bool datasampler_invert = currentdemodulator.DataSlicerParameters.UseDataInversion;

            // the range in seconds we will search for peaks
            double datasampler_second_neg_range = -currentdemodulator.DataSlicerParameters.SearchRange + 2;
            double datasampler_second_pos_range = currentdemodulator.DataSlicerParameters.SearchRange;

            int minutestart_sample = currentdemodulator.MinuteDetectorParameters.Result;

            // the initial range to search for the first 0
            // this should be relatively wide unless the minute-start detector is very accurate
            // the SNR for the first second is usually good, so a wider window doesn't seem to hurt performance
            datasampler_start = minutestart_sample + (int)(currentdemodulator.DataSlicerParameters.SearchFirstMin / decimated_sampleperiod);
            datasampler_stop = minutestart_sample + (int)(currentdemodulator.DataSlicerParameters.SearchFirstMax / decimated_sampleperiod);

            // offset to apply to ratio detected before slicing
            datasampler_ratio_offset = currentdemodulator.DataSlicerParameters.BiasOffset;
            // threshold for slicing, should normally be 1
            datasampler_threshold = currentdemodulator.DataSlicerParameters.Threshold;





            double datasampler_bias_scale = 1;


            double second_sampling_offset = 0;

            double sampler_threshold_autobias_reference = datasampler_threshold;
            // set this carefully
            // this can make the purest of noise become valid data
            // 0.15 removes many minor errors
            // 0.3 fixes a lot
            // 0.5 will likely suppress errors such as wrong status bits etc.
            double sampler_threshold_autobias = currentdemodulator.DataSlicerParameters.AutoBias_Level;//0.15;

            if (currentdemodulator.IsBiased())
            {
                /*
                 * Note for final system:
                 * We can use this solution for fields where we can predict the values.
                 * For mostly static but unknowable fields like leap seconds (and perhaps holidays):
                 *  Keep an average per bit position of the correlation ratio
                 *  Reset an hour change?
                 *  This means we can integrate multiple transmissions of each bit to achieve a better SNR.
                 */
                console_output.AppendFormat("Note: biased with reference bitstream, thresholds {0:F3}/{1:F3}\r\n", sampler_threshold_autobias + datasampler_threshold,
                    (datasampler_threshold - sampler_threshold_autobias));
            }
            else
            {
                console_output.AppendFormat("Threshold is {0:F3}\r\n", datasampler_threshold);
            }

            int secondcount = 0;

            console_output.Append("Bit sample times:\r\n");

            bool firstpass = true;

            while (datasampler_stop < zero_correlation.Length - 1 && secondcount < 59)
            {
                double max_zero = double.NegativeInfinity;
                //double min_zero = double.PositiveInfinity;
                int max_zero_time = 0;
                //int min_zero_time = 0;

                double max_one = double.NegativeInfinity;
                int max_one_time = 0;
                // iterate over the range we expect some data to be and record peaks
                for (int i = datasampler_start; i < datasampler_stop; i++)
                {
                    double current_zero = zero_correlation[i];
                    double current_one = one_correlation[i];
                    // test to improve detection reliability
                    // basically a 3-element correlation on the expected correlation waveform
                    // this improves SNR for good signals
                    // but also seems to make things rapidly go bad when SNR is low, so no good!
                    if (currentdemodulator.DataSlicerParameters.UseFIROffset || currentdemodulator.DataSlicerParameters.UseSymmetryWeight)
                    {
                        double zero_leading_valley = zero_correlation[i - 10];
                        double zero_trailing_valley = zero_correlation[i + 10];
                        double zero_symmetry = Math.Abs(zero_leading_valley - zero_trailing_valley) * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                        double zero_offset = max_zero + (zero_leading_valley + zero_trailing_valley) / 2;

                        double one_leading_valley = one_correlation[i + 10];
                        double one_trailing_valley = one_correlation[i - 10];
                        double one_symmetry = Math.Abs(one_leading_valley - one_trailing_valley);
                        double one_offset = max_one + (one_leading_valley + one_trailing_valley) / 2;

                        // symmetry check might improve performance?
                        if (currentdemodulator.DataSlicerParameters.UseSymmetryWeight)
                        {
                            current_zero -= zero_symmetry * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                            current_one -= one_symmetry * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                        }

                        if (currentdemodulator.DataSlicerParameters.UseFIROffset)
                        {
                            current_zero -= zero_offset * currentdemodulator.DataSlicerParameters.FIROffsetFactor;
                            current_one -= one_offset * currentdemodulator.DataSlicerParameters.FIROffsetFactor;
                        }

                        currentdemodulator.DataSlicerResults.OneWeightedPeaks[secondcount] = max_one;
                        currentdemodulator.DataSlicerResults.ZeroWeightedPeaks[secondcount] = max_zero;
                    }

                    if (current_zero > max_zero)
                    {
                        max_zero = zero_correlation[i];
                        max_zero_time = i;
                    }
                    if (current_one > max_one)
                    {
                        max_one = one_correlation[i];
                        max_one_time = i;
                    }
                }

                // store the value found for zero/one
                currentdemodulator.DataSlicerResults.ZeroPeaks[secondcount] = max_zero;
                currentdemodulator.DataSlicerResults.OnePeaks[secondcount] = max_one;




                // the first second we run is always a 0, assuming we detected the minute correctly
                // the one-detector tends to be more reliable since it's longer, so we try to fudge it a bit
                // we can't make the zero detector any longer while staying in spec
                // for a real-time system we can expect to know what most of the bits are already
                // so we could (very carefully) bias the detector to the expected value
                // obviously this must be done carefully since otherwise we will never update our expectations if something changes
                // we could perhaps do both (i.e. record both an actual reading + the expected), time bits are expected to change linearly, and status bits are expected to
                // be static every hour (and in most cases static for many hours). this can be used to average out errors in most cases.
                if (secondcount == 0 || currentdemodulator.DataSlicerParameters.UseCalibrateAllBits)
                {
                    // with usecalibrateallbits we will recalibrate the bias scale for all known unchanging bits
                    switch (secondcount)
                    {
                        case 0:
                            if (currentdemodulator.DataSlicerParameters.UseInitialZeroCorrection)
                                datasampler_bias_scale = max_zero / max_one;
                            // this check only applies to standard correlation, not the convolvers
                            if (currentdemodulator.DataSlicerParameters.UseTemplateLengthCorrection)
                                datasampler_bias_scale *= (double)currentdemodulator.CorrelatorParameters.ZeroCorrelatorReference.Length / (double)currentdemodulator.CorrelatorParameters.OneCorrelatorReference.Length;
                            break;
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        case 19:
                            datasampler_bias_scale = max_zero / max_one;
                            if (currentdemodulator.DataSlicerParameters.UseTemplateLengthCorrection)
                                datasampler_bias_scale *= (double)currentdemodulator.CorrelatorParameters.ZeroCorrelatorReference.Length / (double)currentdemodulator.CorrelatorParameters.OneCorrelatorReference.Length;
                            break;
                        case 20:
                            //datasampler_bias_scale = max_one / max_zero;
                            break;


                    }
                }

                max_one *= datasampler_bias_scale;


                double ratio = 0;
                // try to handle some edge cases
                if (max_zero <= 0 || max_one <= 0)
                {
                    if (max_zero <= 0)
                        ratio = datasampler_threshold + 1;
                    if (max_one <= 0)
                        ratio = 0;
                }
                else
                {
                    ratio = max_one / max_zero;
                }

                // autobias mode, adds a bias to the threshold based on expected value
                if (currentdemodulator.IsBiased() && secondcount > 0)
                {
                    datasampler_threshold = sampler_threshold_autobias_reference + sampler_threshold_autobias;
                    if (testsignal.Reference_Timecode.GetBitstream()[secondcount])
                        datasampler_threshold = sampler_threshold_autobias_reference - sampler_threshold_autobias;
                }


                ratio += datasampler_ratio_offset;

                // store the ratio for debug analysis
                currentdemodulator.DataSlicerResults.SecondSampleRatios[secondcount] = ratio;

                // at this point we could in future try to do e.g. a 2nd order polynomial curve fit
                // to improve our time resolution

                bool bit = ratio > datasampler_threshold;

                // basically the SNR of this detection, represents the absolute distance to the threshold
                currentdemodulator.DataSlicerResults.RatioVsThreshold[secondcount] = (ratio - datasampler_threshold) * (bit ? 1 : -1);

                if (datasampler_invert)
                    bit = !bit;

                currentdemodulator.DemodulationResult.DemodulatedData[secondcount] = bit;

                int max_time = 0;
                // we now look for the bits within a small time window to improve detection probability
                // we are effectively phase locked now and should get the correct sample point very precisely for all future seconds
                if (bit)
                {
                    max_time = max_one_time;
                }
                else
                {
                    max_time = max_zero_time;
                }

                // print out the decoded bit, time, and bit number
                // this is very useful for debugging since we can quickly look up the relevant bit in the arrayview
                console_output.AppendFormat("{0,2}:{1,6} ", secondcount, max_time);

                // try to improve detection time resolution by doing a second order curve fit
                double[] coeffs;
                double[] xdata = new double[12] { 0,1,2,3,4,5,6,7,8,9,10,11}; // fixed data
                double[] ydata = new double[12];

                if (bit)
                {
                    Array.Copy(one_correlation, max_one_time - 6, ydata, 0, 12);
                }
                else
                {
                    Array.Copy(zero_correlation, max_zero_time - 6, ydata, 0, 12);
                }

                PolynomialRegression.fitIt(ref xdata, ref ydata, 2, out coeffs);

                double max_time_interpolated = max_time - 6 + (-coeffs[1] / (2 * coeffs[2]));

                currentdemodulator.DataSlicerResults.SecondSampleTimes[secondcount] = max_time_interpolated;


                if (currentdemodulator.DataSlicerParameters.UseIntegralTimeDelta)
                {
                    datasampler_start = (int)currentdemodulator.DataSlicerResults.SecondSampleTimes[0] + ((int)(datasampler_second_neg_range / decimated_sampleperiod)) + (secondcount * 200);
                    datasampler_stop = (int)currentdemodulator.DataSlicerResults.SecondSampleTimes[0] + ((int)(datasampler_second_pos_range / decimated_sampleperiod)) + (secondcount * 200);
                }
                else
                {
                    datasampler_start = max_time + (int)(datasampler_second_neg_range / decimated_sampleperiod);
                    datasampler_stop = max_time + (int)(datasampler_second_pos_range / decimated_sampleperiod);
                }

                secondcount++;

                // if we try autothreshold, we just reset back to 0 and change the threshold
                // this is meant to represent a continuous function that keeps track of the
                // ratios for the last few minutes
                // this function is quite aggressive in MeanVariance mode, and can potentially make it demodulate pure noise :)
                if (currentdemodulator.DataSlicerParameters.AutoThreshold != DemodulatorContext.AutoThresholdModes.None && secondcount == 59 && firstpass)
                {
                    double second_sampling_average = 0;
                    double second_sampling_variance = 0;
                    foreach (double d in currentdemodulator.DataSlicerResults.SecondSampleRatios)
                    {
                        second_sampling_average += d;
                        second_sampling_variance += Math.Pow(d, 2);
                    }

                    second_sampling_average /= 59;

                    // try to limit to something slightly sane
                    if (second_sampling_average < 0)
                        second_sampling_average = 0;
                    if (second_sampling_average > 3)
                        second_sampling_average = 3;

                    second_sampling_variance /= 59;
                    second_sampling_variance = Math.Sqrt(second_sampling_variance);

                    if (second_sampling_variance > currentdemodulator.DataSlicerParameters.AutoThresholdMaxBias)
                        second_sampling_variance = currentdemodulator.DataSlicerParameters.AutoThresholdMaxBias;

                    datasampler_threshold = second_sampling_average;
                    sampler_threshold_autobias_reference = datasampler_threshold;

                    if (currentdemodulator.DataSlicerParameters.AutoThreshold == DemodulatorContext.AutoThresholdModes.MeanVariance)
                    {
                        //datasampler_threshold = second_sampling_average + second_sampling_variance / 2;
                        //sampler_threshold_autobias_reference = datasampler_threshold;
                        sampler_threshold_autobias = second_sampling_variance / 2;
                    }

                    console_output.AppendFormat("\r\nAutobias: retrying with threshold {0}\r\n", datasampler_threshold);

                    if (currentdemodulator.IsBiased())
                        console_output.AppendFormat("New autobias thresholds now {0:F4}/{1:F4}\r\n", sampler_threshold_autobias + datasampler_threshold,
                    (datasampler_threshold - sampler_threshold_autobias));

                    secondcount = 1;

                    datasampler_start = (int)currentdemodulator.DataSlicerResults.SecondSampleTimes[0] + (int)(datasampler_second_neg_range / decimated_sampleperiod);
                    datasampler_stop = (int)currentdemodulator.DataSlicerResults.SecondSampleTimes[0] + (int)(datasampler_second_pos_range / decimated_sampleperiod);

                    firstpass = false;
                }
            }

            // if this is a poor recording it's likely the first bit is off, we know it's 0
            if (currentdemodulator.DataSlicerParameters.AutoThreshold != DemodulatorContext.AutoThresholdModes.None)
                currentdemodulator.DemodulationResult.DemodulatedData[0] = false;


            console_output.AppendLine();

            double ratiovsthresholdsum = 0;
            foreach (double d in currentdemodulator.DataSlicerResults.RatioVsThreshold)
                ratiovsthresholdsum += d;
            ratiovsthresholdsum /= 59;

            // kind of useless but let's log it anyway
            console_output.AppendFormat("Ratio vs. threshold average {0:F4}, {1:F4} dB\r\n", ratiovsthresholdsum, Math.Log10(ratiovsthresholdsum) * 10);

            double second_sampling_times_average = 0;
            double second_sampling_times_rms = 0;
            for (int i = 1; i < currentdemodulator.DataSlicerResults.SecondSampleTimes.Length - 2; i++)
            {
                double second_time_delta = currentdemodulator.DataSlicerResults.SecondSampleTimes[i] - currentdemodulator.DataSlicerResults.SecondSampleTimes[i - 1] - 200;
                second_sampling_times_average += second_time_delta;
                second_sampling_times_rms += Math.Pow(second_time_delta, 2);
            }

            second_sampling_times_rms = Math.Sqrt(second_sampling_times_rms)/57;
            second_sampling_times_average /= 57;

            console_output.AppendFormat("Interpolated second average delta error: {0:F4} [ms], RMS {1:F4} [ms]\r\n", (second_sampling_times_average)*1000, (second_sampling_times_rms)*1000);

            console_output.AppendLine();
        }



    }
}
