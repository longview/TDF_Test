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

        private static void Perform_Downconversion(double frequency, double samplerate,
         double[] data, int IQ_decimation_factor,
         double[] i_unfiltered, double[] q_unfiltered,
         double[] i_filtered, double[] q_filtered, ref StringBuilder console_output)
        {

            int averagecount = IQ_decimation_factor;
            // this filter count is fairly flexible, can be reduced without significant reduction in performance
            // can also be increased up to 5x longer without much effect
            // TODO: try out the IIR variants?
            NWaves.Filters.MovingAverageRecursiveFilter i_lpf = new NWaves.Filters.MovingAverageRecursiveFilter(averagecount);
            NWaves.Filters.MovingAverageRecursiveFilter q_lpf = new NWaves.Filters.MovingAverageRecursiveFilter(averagecount);
            console_output.AppendFormat("I/Q moving average filter size {0}\r\n", averagecount);


            double sin_value = Math.Sin(2 * Math.PI * (frequency / samplerate));
            double cos_value = Math.Cos(2 * Math.PI * (frequency / samplerate));
            // iterate over data, doing downconversion
            for (int i = 0; i < data.Length; i++)
            {
                // oscillator definitions
                sin_value = Math.Sin(2 * Math.PI * (frequency / samplerate) * i);
                cos_value = Math.Cos(2 * Math.PI * (frequency / samplerate) * i);

                i_unfiltered[i] = sin_value * data[i];
                q_unfiltered[i] = cos_value * data[i];
            }

            for (int i = 0; i < i_filtered.Length; i++)
            {
                double i_int = 0, q_int = 0;
                for (int j = 0; j < IQ_decimation_factor; j++)
                {
                    i_int += i_lpf.Process((float)i_unfiltered[(i * IQ_decimation_factor) + j]);
                    q_int += q_lpf.Process((float)q_unfiltered[(i * IQ_decimation_factor) + j]);
                }
                // perform low pass filtering
                i_filtered[i] = i_int / IQ_decimation_factor;
                q_filtered[i] = q_int / IQ_decimation_factor;
            }
        }


        private static double Calculate_Signal_SNR(double[] fm_filtered, double fm_filtered_square, int minutestart_sample, ref StringBuilder console_output)
        {
            double FM_Noise_rms = 0;
            double FM_Noise_sum = 0;
            // we can now attempt another SNR calculation, by using the known property of the signal: there is no modulation during the last second of a minute
            // iterate over the filtered data, centered on the minute correlation template
            for (int i = minutestart_sample; i < minutestart_sample + 350 / 2; i++)
            {
                FM_Noise_sum += fm_filtered[i];
            }

            FM_Noise_sum /= minute_correlator_template.Length;

            for (int i = minutestart_sample; i < minutestart_sample + 350; i++)
            {
                FM_Noise_rms += Math.Pow(fm_filtered[i] - FM_Noise_sum, 2);
            }
            FM_Noise_rms /= minute_correlator_template.Length;
            FM_Noise_rms = Math.Sqrt(FM_Noise_rms);


            // ratio of average modulation in the signal
            // to noise (modulation detected during the minute end silence)
            // this calculation is also not great but it is more sensitive
            // I think this is actually a NSR, not a SNR, but it appears to go higher with better SNR now at least :)
            double FM_Rectified_SNR = Math.Abs(FM_Noise_rms / Math.Sqrt(fm_filtered_square));
            double FM_Rectified_SNR_Log = Math.Log10(FM_Rectified_SNR) * 10;



            console_output.AppendFormat("Modulation based SNR = {0}, or {1} dB\r\n", FM_Rectified_SNR, FM_Rectified_SNR_Log);
            return FM_Rectified_SNR;
        }

        private static double[] Generate_Rectified_FM(double[] data, int IQ_decimation_factor, double[] fm_filtered, int movingaveragefilter)
        {
            //NWaves.Filters.MovingAverageFilter fm_rectified_lpf = new NWaves.Filters.MovingAverageFilter(64);
            // try IIR filtering
            NWaves.Filters.MovingAverageRecursiveFilter fm_rectified_lpf = new NWaves.Filters.MovingAverageRecursiveFilter(movingaveragefilter);
            // rectify fm_filtered, might be good?
            double[] fm_filtered_rectified = new double[data.Length / IQ_decimation_factor];
            double fm_filtered_rectified_rms_sum = 0;
            for (int i = 0; i < fm_filtered_rectified.Length; i++)
            {
                fm_filtered_rectified[i] = 1000 * fm_rectified_lpf.Process((float)Math.Pow(fm_filtered[i], 2));
                // store sum for noise calculation
                fm_filtered_rectified_rms_sum += fm_filtered_rectified[i];
            }
            fm_filtered_rectified_rms_sum /= fm_filtered_rectified.Length;

            // also store the standard deviation value of the data for later
            double fm_filtered_rectified_rms = 0;
            for (int i = 0; i < fm_filtered_rectified.Length; i++)
            {
                fm_filtered_rectified[i] = 1000 * fm_rectified_lpf.Process((float)Math.Pow(fm_filtered[i], 2));
                fm_filtered_rectified_rms += Math.Pow(fm_filtered_rectified[i], 2);// - fm_filtered_rectified_rms_sum, 2);
            }

            fm_filtered_rectified_rms /= fm_filtered_rectified.Length;
            fm_filtered_rectified_rms = Math.Sqrt(fm_filtered_rectified_rms);
            return fm_filtered_rectified;
        }

        private static void Perform_PM_Correction(double phase_error_per_sample_vs_frequency,
            ref double[] pm_unfiltered, double[] pm_filtered_drift, ref StringBuilder console_output)
        {
            // perform phase correction for PM; in the real system this will be done as a frequency reference regulator
            // but we simply compute the first order correction since we assume our oscillator is stable
            // we skip the first 1/4 of the data due to filter settling
            double pm_drift = pm_unfiltered[pm_unfiltered.Length - 1] - pm_unfiltered[pm_unfiltered.Length / 4];

            double pm_drift_rate = pm_drift / (pm_unfiltered.Length - pm_unfiltered.Length / 4);

            NWaves.Filters.DcRemovalFilter dc_filter = new DcRemovalFilter(0.995);

            // remove 1st order drift
            for (int i = 0; i < pm_unfiltered.Length; i++)
            {
                pm_filtered_drift[i] = pm_unfiltered[i] - (pm_drift_rate * i);
            }

            double pm_filtered_drift_offset = 0;
            // correct pm filtered data
            for (int i = 0; i < pm_unfiltered.Length; i++)
            {
                // this filter pass seems to have issues, so it leaves an offset
                pm_filtered_drift[i] = dc_filter.Process((float)pm_unfiltered[i]);
                pm_filtered_drift_offset += pm_filtered_drift[i];
            }

            // remove remaining offset
            double pm_drift2 = pm_filtered_drift[pm_filtered_drift.Length - 1] - pm_filtered_drift[pm_filtered_drift.Length / 4];
            double pm_drift_rate2 = pm_drift2 / (pm_filtered_drift.Length - pm_filtered_drift.Length / 4);
            for (int i = 0; i < pm_filtered_drift.Length; i++)
            {
                pm_filtered_drift[i] = pm_filtered_drift[i] / (pm_filtered_drift_offset / pm_filtered_drift.Length);
                //pm_filtered_drift[i] = pm_filtered_drift[i] - (pm_drift_rate2 * i);
            }

            console_output.AppendFormat("Drift calculated, {0} per sample ({1} total)\r\n", pm_drift_rate, pm_drift);
            console_output.AppendFormat("Calculated frequency error: {0}\r\n", pm_drift_rate / phase_error_per_sample_vs_frequency);
        }

        private static double FM_SNR_Calculation(double[] fm_unfiltered, double[] fm_filtered, ref double fm_unfiltered_square,
            ref double fm_filtered_square, ref StringBuilder console_output)
        {
            // this is S^2+N^2
            fm_unfiltered_square /= fm_unfiltered.Length;
            // this is taken to be S^2
            fm_filtered_square /= fm_filtered.Length;

            // therefore we can subtract it
            double SNR_FM_N = Math.Abs(fm_unfiltered_square - fm_filtered_square);

            double SNR_FM = Math.Sqrt(fm_filtered_square / (SNR_FM_N));
            double SNR_FM_Log = 10 * Math.Log10(SNR_FM);

            // this calculation is not super good
            console_output.AppendFormat("FM SNR = {0}, or {1} dB\r\n", SNR_FM, SNR_FM_Log);

            return SNR_FM;
        }

        private static void Demodulate_To_FM(double[] i_filtered, double[] q_filtered, double[] fm_unfiltered, double[] pm_unfiltered, double[] fm_filtered, int movingaverage, out double fm_unfiltered_square, out double fm_filtered_square)
        {
            double qval_last = 0, ival_last = 0;
            double pm_integrator = 0;
            double pm_integrator_filtered = 0;

            NWaves.Filters.MovingAverageRecursiveFilter fm_lpf = new MovingAverageRecursiveFilter(movingaverage);

            // do FM demodulation
            for (int i = 0; i < i_filtered.Length; i++)
            {
                double ival = i_filtered[i];
                double qval = q_filtered[i];

                // derivatives
                double idt = 0, qdt = 0;
                if (i > 0)
                {
                    idt = ival - ival_last;
                    qdt = qval - qval_last;
                }

                ival_last = ival;
                qval_last = qval;

                // perform FM demod
                double ddt_value = (ival * qdt) - (qval * idt);

                // amplitude correct
                double rms = 1 / (Math.Sqrt(Math.Pow(ival, 2) + Math.Pow(qval, 2)));

                // generate output
                double output = 1000 * ddt_value / rms;
                fm_unfiltered[i] = output;
                fm_filtered[i] = fm_lpf.Process((float)output);

                // perform PM integration
                pm_integrator += output;
                pm_unfiltered[i] = pm_integrator;

                pm_integrator_filtered += fm_filtered[i];
                //pm_filtered[i] = pm_integrator_filtered;
            }



            //Console.WriteLine("Finished demodulation");

            // do average value subtraction to remove DC offset

            fm_unfiltered_square = 0;
            fm_filtered_square = 0;
            for (int i = 0; i < fm_unfiltered.Length; i++)
            {
                fm_unfiltered[i] -= pm_integrator / fm_unfiltered.Length;
                fm_unfiltered_square = Math.Pow(fm_unfiltered[i], 2);
                fm_filtered[i] -= pm_integrator_filtered / fm_unfiltered.Length;
                fm_filtered_square = Math.Pow(fm_filtered[i], 2);
            }
        }


    }
}
