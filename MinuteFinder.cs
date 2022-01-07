using NWaves.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    partial class Program
    {
        private static void Find_Minute_Start_Convolver(ref DemodulatorContext demodulator, TestSignalInfo _signal, ref StringBuilder console_output)
        {
            /* Find maximum value and assume this is the start of a minute 
             * Perform a LMS correlation looking for a bunch of zeros
             * This new version uses a convolution filter and some clever weighting of the output waveform to determine the minute start fairly well!
             */
            //bool minutestarted = false;
            double max_minute_correlation = double.NegativeInfinity;
            int minutestart_sample = 0;

            double decimated_sampleperiod = demodulator.DecimatedSamplePeriod;

            double[] minute_correlation_source = demodulator.MinuteDetectorParameters.Source;
            double[] minute_start_correlation = new double[minute_correlation_source.Length];
            double[] minute_convolved = new double[minute_start_correlation.Length];
            double[] minute_convolved_raw = new double[minute_start_correlation.Length];
            double[] minute_convolved_lpf = new double[minute_start_correlation.Length];
            double[] minute_convolved_hpf = new double[minute_start_correlation.Length];
            double[] minute_convolved_weighted = new double[minute_start_correlation.Length];

            // old correlator as first stage
            for (int i = 0; i < minute_correlation_source.Length - minute_correlator_template.Length; i++)
            {

                for (int j = 0; j < minute_correlator_template.Length; j++)
                {
                    minute_start_correlation[i] += -1000 * Math.Pow(minute_correlator_template[j] - minute_correlation_source[i + j], 2);
                }

            }

            List<float> minute_correlation_kernel = new List<float>();

            for (int i = 0; i < 1; i++)
                minute_correlation_kernel.Add(1);
            for (int i = 0; i < 350; i++)
                minute_correlation_kernel.Add(0);
            for (int i = 0; i < 1; i++)
                minute_correlation_kernel.Add(1);

            int convolution_size = demodulator.MinuteDetectorParameters.Convolver_Length;
            int convolution_delay = convolution_size / 2;

            // the number of samples to offset the peak by to make our peak correlation at the start of the minute marker
            // and not the center
            int convolution_peak_offset = (minute_correlation_kernel.Count) + convolution_delay - 490;

            NWaves.Operations.Convolution.OlaBlockConvolver con_minute = new NWaves.Operations.Convolution.OlaBlockConvolver(minute_correlation_kernel.ToArray(), convolution_size);

            NWaves.Filters.MovingAverageRecursiveFilter con_lpf = new MovingAverageRecursiveFilter(400);
            NWaves.Filters.DcRemovalFilter con_hpf = new DcRemovalFilter(0.999);

            // do convolution, offset the start to align it with the input data (max correlation at start of kernel)
            for (int i = convolution_peak_offset; i < minute_convolved_raw.Length + convolution_peak_offset; i++)
            {

                minute_convolved_raw[i - convolution_peak_offset] = con_minute.Process((float)minute_start_correlation[(i > minute_convolved_raw.Length - 1) ? 0 : i]);
                minute_convolved[i - convolution_peak_offset] = minute_convolved_raw[i - convolution_peak_offset];
                // these extra outputs can be useful for debug but are not used
                minute_convolved_hpf[i - convolution_peak_offset] = con_hpf.Process((float)minute_convolved_raw[i - convolution_peak_offset]);
                minute_convolved_lpf[i - convolution_peak_offset] = con_lpf.Process((float)minute_convolved_raw[i - convolution_peak_offset]);
            }

            // search for up to 59 seconds
            // TODO: should also limit it to only searching up to 60 second before the end of the file
            //      since we need a full minute to perform a decode properly

            double weight_factor = demodulator.MinuteDetectorParameters.Weighting_Coefficient;
            int max_minute_search = (int)Math.Min(convolution_peak_offset + 500 + ((double)60 / decimated_sampleperiod), minute_correlation_source.Length);
            for (int i = convolution_peak_offset + 500; i < max_minute_search - 70; i++)
            {
                // bias it towards the distinctive correlation peak.
                double current = minute_convolved[i];
                double leading_valley = (minute_convolved[i - 200] + minute_convolved[i - 190] + minute_convolved[i - 195]) / weight_factor;
                double trailing_valley = (minute_convolved[i + 200] + minute_convolved[i + 190] + minute_convolved[i + 195]) / weight_factor;

                double offset = current + (leading_valley + trailing_valley) / 2;

                // try to shift everything to be symmetric
                //current += offset;
                //leading_valley += offset;
                //trailing_valley += offset;

                double weighted_correlation = current + (Math.Abs(current - leading_valley) + Math.Abs(current - trailing_valley));
                // weight for valleys
                //weighted_correlation += Math.Abs(minute_start_correlation[i - 300]);// + Math.Abs(minute_start_correlation[i + 200]);

                weighted_correlation += offset;
                // use weighted correlation
                // weight further by average signal level
                minute_convolved_weighted[i] = weighted_correlation;

                //minute_weighted_correlation[i] = weighted_correlation;
                if (weighted_correlation > max_minute_correlation)
                {
                    max_minute_correlation = weighted_correlation;// minute_start_correlation[i];
                    minutestart_sample = i;
                }
            }

            demodulator.MinuteDetectorParameters.CorrelationOutput = minute_convolved;
            demodulator.MinuteDetectorParameters.WeightedOutput = minute_convolved_weighted;

            // note that the recordings often actually start a second or two after the timestamp
            // due to how SDR-Console works
            console_output.AppendFormat("Found start of minute at time {0} ({1}), expected {2} ({3})\r\n", decimated_sampleperiod * minutestart_sample, minutestart_sample,
                _signal.ExpectedMinuteStartSeconds, _signal.ExpectedMinuteStartSeconds / decimated_sampleperiod);
            demodulator.MinuteDetectorParameters.Result = minutestart_sample;
        }

        private static void Find_Minute_Start_Correlator(ref DemodulatorContext demodulator, TestSignalInfo _signal, ref StringBuilder console_output)
        {
            /* Find maximum value and assume this is the start of a minute 
             * Perform a LMS correlation looking for a bunch of zeros
             * This new version uses a convolution filter and some clever weighting of the output waveform to determine the minute start fairly well!
             */
            //bool minutestarted = false;
            double max_minute_correlation = double.NegativeInfinity;
            int minutestart_sample = 0;

            double decimated_sampleperiod = demodulator.DecimatedSamplePeriod;

            double[] minute_correlation_source = demodulator.MinuteDetectorParameters.Source;
            double[] minute_start_correlation = new double[minute_correlation_source.Length];
            double[] minute_correlated = new double[minute_start_correlation.Length];
            double[] minute_correlated_raw = new double[minute_start_correlation.Length];
            demodulator.MinuteDetectorParameters.WeightedOutput = new double[minute_start_correlation.Length];

            // correlation by offset corrected RMS
            double minute_correlation_sum = 0;
            for (int i = minute_correlator_template.Length; i < minute_correlation_source.Length; i++)
            {

                for (int j = 0; j < minute_correlator_template.Length; j++)
                {
                    minute_correlated_raw[i- minute_correlator_template.Length] += -1*Math.Pow(minute_correlation_source[i - j], 2);
                    // this is wrong
                    minute_correlation_sum += minute_correlated_raw[i - minute_correlator_template.Length];
                }

            }

            // this isn't quite right, but it is working and it broke last time I tried to fix it
            for (int i = 0; i < minute_correlated_raw.Length; i++)
            {
                minute_correlated[i] = minute_correlated_raw[i] - (minute_correlation_sum / minute_correlated_raw.Length* minute_correlator_template.Length);
                minute_correlated[i] = Math.Sqrt(minute_correlated[i]);
            }

            // the number of samples to offset the peak by to make our peak correlation at the start of the minute marker
            // and not the center
            int convolution_peak_offset = (minute_correlator_template.Length);

            // search for up to 59 seconds
            double weight_factor = demodulator.MinuteDetectorParameters.Weighting_Coefficient;
            int max_minute_search = (int)Math.Min(convolution_peak_offset + 500 + ((double)60 / decimated_sampleperiod), minute_correlation_source.Length);
            for (int i = convolution_peak_offset; i < max_minute_search - 70; i++)
            {
                // bias it towards the distinctive correlation peak.
                double current = minute_correlated[i];

                current += minute_correlated[i - 50] * -0.2;

                demodulator.MinuteDetectorParameters.WeightedOutput[i] = current;

                //minute_weighted_correlation[i] = weighted_correlation;
                if (current > max_minute_correlation)
                {
                    max_minute_correlation = current;// minute_start_correlation[i];
                    minutestart_sample = i+demodulator.MinuteDetectorParameters.ResultOffset;
                }
            }

            demodulator.MinuteDetectorParameters.CorrelationOutput = minute_correlated;

            // note that the recordings often actually start a second or two after the timestamp
            // due to how SDR-Console works
            console_output.AppendFormat("Found start of minute at time {0} ({1}), expected {2} ({3})\r\n", decimated_sampleperiod * minutestart_sample, minutestart_sample,
                _signal.ExpectedMinuteStartSeconds, _signal.ExpectedMinuteStartSeconds / decimated_sampleperiod);
            demodulator.MinuteDetectorParameters.Result = minutestart_sample;
        }


    }
}
