using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TDF_Test
{
    partial class Program
    {
        public struct TestSignalInfo
        {
            public TestSignalInfo(string _filepath, double _frequency, string _comment, double _snr, Station_Status _status, DateTime _date)
            {
                FilePath = _filepath;
                Comment = _comment;
                SNR = _snr;
                Frequency = _frequency;
                Status = _status;
                // add 1 minute to timestamp from start of recording timestamp
                Recorded_Timestamp_UTC = _date.AddMinutes(1);
            }
            public string FilePath;
            public string Comment;
            public double Frequency;
            public double SNR;
            public Station_Status Status;
            public DateTime Recorded_Timestamp_UTC;
            public enum Station_Status
            {
                OnAir,
                Maintenance
            }
        }

        static void Main(string[] args)
        {


            int testindex = 5;

            List<TestSignalInfo> testsignals = new List<TestSignalInfo>();
            // input file must be mono 16-bit, 20000 Hz (oddball rate)
            testsignals.Add(new TestSignalInfo("..\\..\\websdr_recording_start_2021-12-28T12_57_51Z_157.0kHz.wav", 5000,
                "webSDR recording, high quality", 70, TestSignalInfo.Station_Status.OnAir, new DateTime(2021, 12, 28, 12, 58, 00)));

            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T163350Z, 157 kHz, Wide-U.wav", 5000,
                "Ok signal, mid day", 30, TestSignalInfo.Station_Status.OnAir, new DateTime(2021, 12, 29, 16, 34, 00)));

            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T185106Z, 157 kHz, Wide-U.wav", 5000,
                "Good signal, evening", 40, TestSignalInfo.Station_Status.OnAir, new DateTime(2021, 12, 29, 18, 52, 00)));

            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T090027Z, 157 kHz, Wide-U.wav", 5000,
                "Medium signal, morning", 22, TestSignalInfo.Station_Status.OnAir, new DateTime(2021, 12, 30, 09, 01, 00)));

            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T102229Z, 157 kHz, Wide-U.wav", 5000,
                "Maintenance phase, off air", 0, TestSignalInfo.Station_Status.Maintenance, new DateTime(2021, 12, 30, 10, 23, 00)));

            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T105034Z, 157 kHz, Wide-U.wav", 5000,
                "Medium signal, morning", 20, TestSignalInfo.Station_Status.OnAir, new DateTime(2021, 12, 30, 10, 51, 00)));

            Console.WriteLine("Using test index {0}.\r\nFile {1} (IF = {6}\r\nSNR {2}, station was {3}.\r\nTime transmitted: {4}.\r\nComment: {5}",
                testindex, testsignals[testindex].FilePath, testsignals[testindex].SNR, 
                testsignals[testindex].Status == TestSignalInfo.Station_Status.OnAir ? "on air" : "off air",
                testsignals[testindex].Recorded_Timestamp_UTC.ToString("o"), testsignals[testindex].Comment, testsignals[testindex].Frequency);

            TestSignalInfo testsignal_current = testsignals[testindex];

            // frequency offset of USB receiver
            double frequency = testsignal_current.Frequency;

            // TODO: this is wrong, fix it
            double phase_error_per_sample_vs_frequency = (0.6035457 + -0.1047503) / 10;

            double samplerate = 20000;
            double sampleperiod = 1 / samplerate;

            double[] data;
            double[] data_right;
            openWav(testsignals[testindex].FilePath, out data, out data_right);

            Console.WriteLine("Read file, length {0} samples, {1} seconds", data.Length, data.Length / samplerate);

            // debug time register to allow plotting in time
            double[] timescale = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                timescale[i] = i * sampleperiod;
            }

            int IQ_decimation_factor = 100;
            double decimated_sampleperiod = IQ_decimation_factor * sampleperiod;

            // output buffer for I/Q data
            double[] i_unfiltered = new double[data.Length];
            double[] q_unfiltered = new double[data.Length];

            double[] i_filtered = new double[data.Length / IQ_decimation_factor];
            double[] q_filtered = new double[data.Length / IQ_decimation_factor];

            double[] fm_unfiltered = new double[data.Length / IQ_decimation_factor];
            double[] pm_unfiltered = new double[data.Length / IQ_decimation_factor];

            double[] fm_filtered = new double[data.Length / IQ_decimation_factor];
            double[] pm_filtered = new double[data.Length / IQ_decimation_factor];
            double[] pm_filtered_drift = new double[data.Length / IQ_decimation_factor];

            double[] timescale_decimated = new double[data.Length / IQ_decimation_factor];

            for (int i = 0; i < timescale_decimated.Length; i++)
            {
                timescale_decimated[i] = i * sampleperiod * IQ_decimation_factor;
            }

            Console.WriteLine("Using sample rate {0}, output decimation {1}, beginning IQ conversion", samplerate, IQ_decimation_factor);

            NWaves.Filters.MovingAverageFilter i_lpf = new NWaves.Filters.MovingAverageFilter(IQ_decimation_factor);
            NWaves.Filters.MovingAverageFilter q_lpf = new NWaves.Filters.MovingAverageFilter(IQ_decimation_factor);

            NWaves.Filters.MovingAverageFilter fm_lpf = new NWaves.Filters.MovingAverageFilter(8);
            NWaves.Filters.MovingAverageFilter fm_rectified_lpf = new NWaves.Filters.MovingAverageFilter(64);

            // generate low pass filter for I/Q data
            //MathNet.Filtering.OnlineFilter i_lpf = MathNet.Filtering.FIR.OnlineFirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Finite, samplerate, 50, 2);
            //MathNet.Filtering.OnlineFilter i_lpf = MathNet.Filtering.IIR.OnlineIirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Infinite, samplerate, 50);
            //MathNet.Filtering.OnlineFilter q_lpf = MathNet.Filtering.FIR.OnlineFirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Finite, samplerate, 50, 2);
            //MathNet.Filtering.OnlineFilter q_lpf = MathNet.Filtering.IIR.OnlineIirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Infinite, samplerate, 50);



            //MathNet.Filtering.OnlineFilter fm_lpf = MathNet.Filtering.FIR.OnlineFirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Finite, samplerate/ IQ_decimation_factor, 50, 2);
            //MathNet.Filtering.OnlineFilter fm_lpf = MathNet.Filtering.IIR.OnlineIirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Infinite, samplerate, 50);

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

            Console.WriteLine("Performing FM demodulation");

            double qval_last = 0, ival_last = 0;
            double pm_integrator = 0;
            double pm_integrator_filtered = 0;

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



            Console.WriteLine("Finished demodulation");

            // do average value subtraction to remove DC offset

            double fm_unfiltered_square = 0;
            double fm_filtered_square = 0;
            for (int i = 0; i < fm_unfiltered.Length; i++)
            {
                fm_unfiltered[i] -= pm_integrator / fm_unfiltered.Length;
                fm_unfiltered_square = Math.Pow(fm_unfiltered[i], 2);
                fm_filtered[i] -= pm_integrator_filtered / fm_unfiltered.Length;
                fm_filtered_square = Math.Pow(fm_filtered[i], 2);
            }

            // attempt an SNR calculation based on full band (signal and noise)
            // and filtered square values (mostly just signal we assume)

            // this is S^2+N^2
            fm_unfiltered_square /= fm_unfiltered.Length;
            // this is taken to be S^2
            fm_filtered_square /= fm_filtered.Length;

            // therefore we can subtract it
            double SNR_FM_N = Math.Abs(fm_unfiltered_square - fm_filtered_square);

            double SNR_FM = Math.Sqrt(fm_filtered_square / (SNR_FM_N));
            double SNR_FM_Log = 10 * Math.Log10(SNR_FM);

            // this calculation is not super good
            Console.WriteLine("FM SNR = {0}, or {1} dB", SNR_FM, SNR_FM_Log);


            // perform phase correction for PM; in the real system this will be done as a frequency reference regulator
            // but we simply compute the first order correction since we assume our oscillator is stable
            // we skip the first 1/4 of the data due to filter settling
            double pm_drift = pm_unfiltered[pm_unfiltered.Length - 1] - pm_unfiltered[pm_unfiltered.Length / 4];

            //for (int i = 0; i < pm_filtered.Length; i++)
            //{
            //    pm_drift += pm_filtered_drift[i];
            //}
            //double pm_drift_rate = pm_drift / pm_filtered.Length;
            // average drift per sample
            double pm_drift_rate = pm_drift / (pm_unfiltered.Length - pm_unfiltered.Length / 4);

            // correct pm filtered data
            for (int i = 0; i < pm_unfiltered.Length; i++)
            {
                pm_filtered_drift[i] = pm_unfiltered[i] - (pm_drift_rate * i);
            }
            Console.WriteLine("Drift corrected, {0} per sample ({1} total)", pm_drift_rate, pm_drift);
            Console.WriteLine("Calculated frequency error: {0}", pm_drift_rate / phase_error_per_sample_vs_frequency);


            // rectify fm_filtered, might be good?
            fm_lpf.Reset();
            double[] fm_filtered_rectified = new double[data.Length / IQ_decimation_factor];
            double fm_filtered_rectified_rms_sum = 0;
            for (int i = 0; i < fm_filtered_rectified.Length; i++)
            {
                fm_filtered_rectified[i] = 1000*fm_rectified_lpf.Process((float)Math.Pow(fm_filtered[i],2));
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



            StringBuilder correlation_output = new StringBuilder();

            // generate correlation templates, these are valid for the first websdr file (with fantastic SNR)
            int correlator1_template_offset = 1725;
            for (int i = correlator1_template_offset - 28; i < correlator1_template_offset + 28; i++)
            {
                correlation_output.AppendFormat("{0},", fm_filtered[i]);
            }

            File.WriteAllText("correlation_1.txt", correlation_output.ToString());

            correlation_output.Clear();
            int correlator2_template_offset = 2926;
            // generate correlation template
            for (int i = correlator2_template_offset - 25; i < correlator2_template_offset + 43; i++)
            {
                correlation_output.AppendFormat("{0},", fm_filtered[i]);
            }

            File.WriteAllText("correlation_2.txt", correlation_output.ToString());

            correlation_output.Clear();
            // generate correlation template
            for (int i = 1475; i < 1700; i++)
            {
                correlation_output.AppendFormat("{0},", 0);// fm_filtered_rectified[i]);
            }

            File.WriteAllText("correlation_3.txt", correlation_output.ToString());

            /* The technique for correlation here is to template match using least square error matching
             * i.e. we are sensitive to the exact amplitude, not just the shape
             * We first find the start of a minute using template 3 on the *rectified* FM data
             * this then determines where we look in the bit 0 or 1 sets
             */


            Console.WriteLine("Doing correlation for start of minute");
            double[] minute_start_correlation = new double[fm_filtered.Length];
            double minute_start_correlation_sum = 0;

            // correlation for minute start
            // this should perhaps not be a correlator?
            for (int i = 0; i < fm_filtered_rectified.Length - minute_correlator_template.Length; i++)
            {
                for (int j = 0; j < minute_correlator_template.Length; j++)
                {
                    minute_start_correlation[i] += -Math.Pow(minute_correlator_template[j] - fm_filtered_rectified[i + j], 2);
                }

                minute_start_correlation_sum += minute_start_correlation[i];

            }

            minute_start_correlation_sum /= minute_start_correlation.Length;


            Console.WriteLine("Doing first correlation for bits 0");

            double[] zero_correlation = new double[fm_filtered.Length];
            double zero_correlation_sum = 0;

            // correlation 1, look for second start with data 0
            for (int i = 0; i < fm_filtered.Length - zero_correlator_template.Length; i++)
            {
                for (int j = 0; j < zero_correlator_template.Length; j++)
                {
                    //correlation1[i] += correlator_1[j] * fm_filtered[i + j];
                    zero_correlation[i] += -Math.Pow(zero_correlator_template[j] - fm_filtered[i + j], 2);
                    //correlation1[i] += (correlator_1[j] > 0 ? 1 : 0) * fm_filtered[i + j];
                    //correlation1[i] += (correlator_1[j] > 0 ? 0:1) ^ (fm_filtered[i + j] > 0 ? 1 : 0);
                }
                zero_correlation_sum += zero_correlation[i];

            }
            zero_correlation_sum /= zero_correlation.Length;

            Console.WriteLine("Doing correlation for bits 1");
            double[] one_correlation = new double[fm_filtered.Length];
            double one_correlation_sum = 0;

            // correlation 1, look for second start with data 0
            for (int i = 0; i < fm_filtered.Length - one_correlator_template.Length; i++)
            {
                for (int j = 0; j < one_correlator_template.Length; j++)
                {
                    //correlation1[i] += correlator_1[j] * fm_filtered[i + j];
                    one_correlation[i] += -Math.Pow(one_correlator_template[j] - fm_filtered[i + j], 2);
                    //correlation1[i] += (correlator_1[j] > 0 ? 1 : 0) * fm_filtered[i + j];
                    //correlation1[i] += (correlator_1[j] > 0 ? 0:1) ^ (fm_filtered[i + j] > 0 ? 1 : 0);
                }

                one_correlation_sum += one_correlation[i];

            }

            one_correlation_sum /= one_correlation.Length;

            // offset correct the correlators
            for (int i = 0; i < zero_correlation.Length; i++)
            {
                zero_correlation[i] -= zero_correlation_sum;
                one_correlation[i] -= one_correlation_sum;
                minute_start_correlation[i] -= minute_start_correlation_sum;
            }

            /* Find maximum value
             */
            //bool minutestarted = false;
            double max_minute_correlation = double.NegativeInfinity;
            int minutestart_sample = 0;
            // search for up to 59 seconds
            // TODO: should also limit it to only searching up to 60 second before the end of the file
            //      since we need a full minute to perform a decode properly
            int max_minute_search = (int)Math.Min(((double)59 / decimated_sampleperiod), fm_unfiltered.Length);
            for (int i = 0; i < max_minute_search; i++)
            {
                if (minute_start_correlation[i] > max_minute_correlation)
                {
                    max_minute_correlation = minute_start_correlation[i];
                    minutestart_sample = i;
                }
                /*if (correlation3[i] > -1 && !minutestarted)
                {
                    minutestartedsample = i;
                    minutestarted = true;
                    continue;
                }
                if (correlation3[i] < -1 && minutestarted)
                {
                    minutestart_sample = minutestartedsample + (i - minutestartedsample);
                    break;
                }*/
            }

            Console.WriteLine("Found start of minute at time {0}", decimated_sampleperiod * minutestart_sample);

            double FM_Noise_rms = 0;
            double FM_Noise_sum = 0;
            // we can now attempt another SNR calculation, by using the known property of the signal: there is no modulation during the last second of a minute
            // iterate over the filtered data, centered on the minute correlation template
            for (int i = minutestart_sample-minute_correlator_template.Length/2; i < minutestart_sample + minute_correlator_template.Length / 2; i++)
            {
                FM_Noise_sum += fm_filtered[i];
            }

            FM_Noise_sum /= minute_correlator_template.Length;

            for (int i = minutestart_sample - minute_correlator_template.Length / 2; i < minutestart_sample + minute_correlator_template.Length / 2; i++)
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

            Console.WriteLine("Modulation based SNR = {0}, or {1} dB", FM_Rectified_SNR, FM_Rectified_SNR_Log);

            // this should be relatively wide unless the minute-start detector is very accurate
            // the SNR for the first second is usually good, so a wider window doesn't seem to hurt performance
            int datasampler_start = minutestart_sample + (int)(0.75 / decimated_sampleperiod);
            int datasampler_stop = minutestart_sample + (int)(1.0 / decimated_sampleperiod);
            int secondcount = 0;

            Console.Write("Next bit expected at: ({1}){0}", (datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod, 0);

            List<bool> payload_data = new List<bool>();
            List<double> second_sampling_times = new List<double>();

            double datasampler_bias = 0;

            while (datasampler_stop < fm_unfiltered.Length - 1 && secondcount < 59)
            {
                double max_zero = double.NegativeInfinity;
                int max_zero_time = 0;

                double max_one = double.NegativeInfinity;
                int max_one_time = 0;
                // iterate over the range we expect some data to be and record peaks
                for (int i = datasampler_start; i < datasampler_stop; i++)
                {
                    if (zero_correlation[i] > max_zero)
                    {
                        max_zero = zero_correlation[i];
                        max_zero_time = i;
                    }

                    if (one_correlation[i] > max_one)
                    {
                        max_one = one_correlation[i];
                        max_one_time = i;
                    }
                }

                // the first second we run is always a 0, assuming we detected the minute correctly
                // the one-detector tends to be more reliable since it's longer, so we try to fudge it a bit
                // we can't make the zero detector any longer while staying in spec
                if (secondcount == 0)
                {
                    // correct based on first measurement
                    datasampler_bias = max_zero / max_one;
                    // correct manually to make it work better in poor SNR
                    datasampler_bias *= 0.90;
                    // correct for template length
                    datasampler_bias *= (double)zero_correlator_template.Length / (double)one_correlator_template.Length;
                }

                max_one *= datasampler_bias;

                // at this point we could in future try to do e.g. a 2nd order polynomial curve fit
                // to improve our time resolution

                bool bit = max_one > max_zero;

                payload_data.Add(bit);

                // we now look for the bits within a small time window to improve detection probability
                // we are effectively phase locked now and should get the correct sample point very precisely for all future seconds
                if (bit)
                {
                    datasampler_start = max_one_time + (int)(0.95 / decimated_sampleperiod);
                    datasampler_stop = max_one_time + (int)(1.05 / decimated_sampleperiod);
                }
                else
                {
                    datasampler_start = max_zero_time + (int)(0.95 / decimated_sampleperiod);
                    datasampler_stop = max_zero_time + (int)(1.05 / decimated_sampleperiod);
                }

                // print out the decoded bit, time, and bit number
                // this is very useful for debugging since we can quickly look up the relevant bit in the arrayview
                Console.Write(":{2} ({1}){0}", (datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod, secondcount+1, bit ? "1" : "0");
                // add all but the first second sample point (first one is usually slightly off)
                second_sampling_times.Add((datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod);

                secondcount++;
            }

            Console.WriteLine("");

            double second_delta_rms = 0;
            for (int i = 1; i < second_sampling_times.Count; i++)
            {
                second_delta_rms += second_sampling_times[i] - second_sampling_times[i - 1];
            }
            second_delta_rms /= second_sampling_times.Count - 1;
            //second_delta_rms = Math.Abs(1-second_delta_rms);


            Console.WriteLine("Second delta average: {0} ms", second_delta_rms*1000);

            Console.Write("Decoded {0} bits: ", payload_data.Count, datasampler_stop * decimated_sampleperiod);
            foreach (bool bit in payload_data)
            {
                Console.Write("{0}", bit ? 1 : 0);
            }
            Console.WriteLine("");

            int decode_error_count = 0;

            if (payload_data.Count != 59)
                decode_error_count++;

            // let's parse the data!
            Console.WriteLine(payload_data[0] ? "First bit error" : "First bit ok");
            if (payload_data[0])
                decode_error_count++;

            Console.WriteLine(payload_data[1] ? "Positive Leap Warning" : "No Pos Leap");
            Console.WriteLine(payload_data[2] ? "Negative Leap Warning" : "No Neg Leap");

            int hammingweight = (payload_data[3] ? 2 : 0) + (payload_data[4] ? 4 : 0) + (payload_data[5] ? 8 : 0) + (payload_data[6] ? 16 : 0);
            int hammingcount = 0;
            for (int i = 21; i < 59; i++)
            {
                if (payload_data[i])
                    hammingcount++;
            }

            Console.WriteLine("Hamming weight 21-58 is {0}, I count {1}, this is {2}", hammingweight, hammingcount, hammingcount == hammingweight ? "good!":"bad :(");
            if (hammingcount != hammingweight)
                decode_error_count++;

            if (!payload_data[7] && !payload_data[8] && !payload_data[9] && !payload_data[10] && !payload_data[11] && !payload_data[12])
            {
                Console.WriteLine("Unused bits 7-12 ok!");
            }
            else
                decode_error_count++;

            Console.WriteLine(payload_data[13] ? "Tomorrow is a public holiday!" : "No holiday tomorrow");
            Console.WriteLine(payload_data[14] ? "Today is a public holiday!" : "No holiday today :(");
            Console.WriteLine(payload_data[15] ? "Bit 15 is high, ignored" : "Bit 15 is low, ignored");
            Console.WriteLine(payload_data[16] ? "Time zone will change at the next hour mark." : "Time zone will not change at the next hour mark");
            Console.WriteLine(payload_data[17] ? "Currently using CEST" : "Not using CEST");
            Console.WriteLine(payload_data[18] ? "Currently using CET" : "Not using CET");

            // No spec, but these seem like they should never happen?
            if (payload_data[17] && payload_data[18])
            {
                decode_error_count++;
                Console.WriteLine("Error: Using both CET and CEST, how?");
            }
            else if (!payload_data[17] && !payload_data[18])
            {
                decode_error_count++;
                Console.WriteLine("Error: Not using CET, nor CEST, how?");
            }

            // for the C# version we known about the timezone in use, so we just set it
            TimeZoneInfo decoded_tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");


            Console.WriteLine(payload_data[19] ? "Unused bit 19 is high, error" : "Unused bit 19 ok");
            if (payload_data[19])
                decode_error_count++;
            Console.WriteLine(payload_data[20] ? "Start of time ok" : "Start of time framing error");
            if (!payload_data[20])
                decode_error_count++;

            // this format is very french, and does not use true binary, rather is switches to 10/20/40/80 weights after weight 8
            // we need to watch it with parity calculations since the number of set bits could be different before/after conversion to normal binary?
            int minutes = (payload_data[21] ? 1 : 0) + (payload_data[22] ? 2 : 0) + (payload_data[23] ? 4 : 0) + (payload_data[24] ? 8 : 0) + (payload_data[25] ? 10 : 0) +
                (payload_data[26] ? 20 : 0) + (payload_data[27] ? 40 : 0);

            int paritycount = 0;
            for (int i = 21; i < 28; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            Console.WriteLine(paritycount % 2 == 1 == payload_data[28] ? "Minute parity ok" : "Minute parity error");

            if (paritycount % 2 == 1 != payload_data[28])
                decode_error_count++;




            int hours = (payload_data[29] ? 1 : 1) + (payload_data[30] ? 2 : 0) + (payload_data[31] ? 4 : 0) + (payload_data[32] ? 8 : 0) + (payload_data[33] ? 10 : 0) +
                (payload_data[34] ? 20 : 0);

            paritycount = 0;
            for (int i = 29; i < 35; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            Console.WriteLine(paritycount % 2 == 1 == payload_data[35] ? "Hours parity ok" : "Hours parity error");
            if (paritycount % 2 == 1 != payload_data[35])
                decode_error_count++;

            int day_of_month = (payload_data[36] ? 1 : 0) + (payload_data[37] ? 2 : 0) + (payload_data[38] ? 4 : 0) + (payload_data[39] ? 8 : 0) + (payload_data[40] ? 10 : 0) +
                (payload_data[41] ? 20 : 0);
            int day_of_week = (payload_data[42] ? 1 : 0) + (payload_data[43] ? 2 : 0) + (payload_data[44] ? 4 : 0);
            int month = (payload_data[45] ? 1 : 0) + (payload_data[46] ? 2 : 0) + (payload_data[47] ? 4 : 0) + (payload_data[48] ? 8 : 0) + (payload_data[49] ? 10 : 0);
            int year = (payload_data[50] ? 1 : 0) + (payload_data[51] ? 2 : 0) + (payload_data[52] ? 4 : 0) + (payload_data[53] ? 8 : 0) + (payload_data[54] ? 10 : 0) +
                (payload_data[55] ? 20 : 0) + (payload_data[56] ? 40 : 0) + (payload_data[57] ? 80 : 0);

            paritycount = 0;
            for (int i = 36; i < 58; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            Console.WriteLine(paritycount % 2 == 1 == payload_data[58] ? "Date bits parity ok" : "Date bits parity error");
            if (paritycount % 2 == 1 != payload_data[58])
                decode_error_count++;

            Console.WriteLine("At the next minute marker: {0}:{1}, day of month {2}, day of week {3}, month {4}, year is 20{5}", hours, minutes,day_of_month, day_of_week, month, year);

            // this conversion "knows" that we are in the same time zone as the transmitter; this is not guaranteed
            // should use the timezone info decoded above
            // put this in a try/catch since we don't know if the datetime object will be valid
            try
            {
                DateTime decoded_time = new DateTime(year + 2000, month, day_of_month, hours, minutes, 0, DateTimeKind.Local);
                DateTimeOffset decoded_offset_time = new DateTimeOffset(decoded_time, decoded_tz.GetUtcOffset(decoded_time));
                Console.Write("Decoded time is valid: ");
                Console.Write(decoded_offset_time.UtcDateTime.ToString("o"));
                Console.WriteLine(" and locally {0}", decoded_time.ToString("o"));

                // check if the time is equal to the test recording info
                if (testsignal_current.Recorded_Timestamp_UTC.CompareTo(decoded_time.ToUniversalTime()) == 0)
                    Console.WriteLine("Decoded time matches recording timestamp.");
                else
                {
                    Console.WriteLine("Decoded time does not match timestamp.");
                    decode_error_count++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Decoded date and time is not valid.");
                decode_error_count++;
            }
            Console.WriteLine("Decoded with {0} (detectable) errors", decode_error_count);
            Console.WriteLine("Finished");

            Console.ReadLine();
        }

        /* Function to get parity of number n.
    It returns 1 if n has odd parity, and
    returns 0 if n has even parity */
        static bool getParity(int n)
        {
            bool parity = false;
            while (n != 0)
            {
                parity = !parity;
                n = n & (n - 1);
            }
            return parity;

        }

        static int bitcount(int a)
        {
            byte[] numberAsByte = new byte[] { (byte)a };
            BitArray bits = new BitArray(numberAsByte);
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (bits[i])
                {
                    count++;
                }
            }

            return count;
        }



        // convert two bytes to one double in the range -1 to 1
        static double bytesToDouble(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0;
        }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        static void openWav(string filename, out double[] left, out double[] right)
        {
            byte[] wav = File.ReadAllBytes(filename);

            // Determine if mono or stereo
            int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            int samples = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
            if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            left = new double[samples];
            if (channels == 2) right = new double[samples];
            else right = null;

            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                left[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
                if (channels == 2)
                {
                    right[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
        }
    }
}
