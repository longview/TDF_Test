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


        private static void Correlate(ref DemodulatorContext currentdemodulator, ref StringBuilder console_output)
        {
            /* The technique for correlation here is to template match using least square error matching
                         * i.e. we are sensitive to the exact amplitude, not just the shape
                         * Also supports convolution, but this appears to offer no benefit vs. LMS correlation here
                         * TODO: split this out to avoid duplicating code for one/zero correlation
                         */

            double[] _zero_correlator = currentdemodulator.CorrelatorParameters.ZeroCorrelatorReference;
            double[] _one_correlator = currentdemodulator.CorrelatorParameters.OneCorrelatorReference;
            double[] data_correlation_source = currentdemodulator.CorrelatorParameters.DemodulatorSource;
            currentdemodulator.CorrelatorParameters.ZeroDemodulatorResult = new double[data_correlation_source.Length];
            currentdemodulator.CorrelatorParameters.OneDemodulatorResult = new double[data_correlation_source.Length];
            double[] one_correlation = currentdemodulator.CorrelatorParameters.OneDemodulatorResult;
            double[] zero_correlation = currentdemodulator.CorrelatorParameters.ZeroDemodulatorResult;


            console_output.AppendFormat("Doing correlations in {0} mode.\r\n", currentdemodulator.ToString());

            NWaves.Operations.Convolution.OlaBlockConvolver con_zero = null;
            NWaves.Operations.Convolution.OlaBlockConvolver con_one = null;
            int kernelsize = currentdemodulator.CorrelatorParameters.KernelLength;

            int kerneldelay_zero = currentdemodulator.CorrelatorParameters.CommonOffset + currentdemodulator.CorrelatorParameters.ZeroOffset;
            int kerneldelay_one = currentdemodulator.CorrelatorParameters.CommonOffset + currentdemodulator.CorrelatorParameters.OneOffset;

            if (currentdemodulator.IsConvolver())
            {
                // initialize convolvers if needed   
                float[] convolver_kernel_zero = new float[_zero_correlator.Length];
                for (int i = 0; i < _zero_correlator.Length; i++)
                {
                    convolver_kernel_zero[i] = (float)_zero_correlator[i];
                }

                con_zero = new NWaves.Operations.Convolution.OlaBlockConvolver(convolver_kernel_zero, kernelsize);


                float[] convolver_kernel_one = new float[_one_correlator.Length];
                for (int i = 0; i < _one_correlator.Length; i++)
                {
                    convolver_kernel_one[i] = (float)_one_correlator[i];
                }

                con_one = new NWaves.Operations.Convolution.OlaBlockConvolver(convolver_kernel_one, kernelsize);

            }

            //NWaves.Filters.DcRemovalFilter input_hpf = new DcRemovalFilter(currentdemodulator.CorrelatorParameters.InputHighPassFilterCoefficient);

            if (currentdemodulator.CorrelatorParameters.UseInputHighPassFiltering)
            {
                NWaves.Filters.DcRemovalFilter input_hpf = new DcRemovalFilter(currentdemodulator.CorrelatorParameters.InputHighPassFilterCoefficient);


                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    data_correlation_source[i] = input_hpf.Process((float)data_correlation_source[i]);
                }
            }

            double correlation_scale = -1;

            zero_correlation = new double[data_correlation_source.Length];
            double zero_correlation_sum = 0;
            double zero_correlation_min = double.PositiveInfinity;

            bool reverse_correlators = currentdemodulator.CorrelatorParameters.TimeReverseCorrelators;

            // correlation for zero bits
            for (int i = 0; i < data_correlation_source.Length - _zero_correlator.Length; i++)
            {
                if (currentdemodulator.IsConvolver() && con_zero != null)
                {
                    // dump the kernel size to avoid delay
                    int j = i - kerneldelay_zero;

                    zero_correlation[j < 0 ? 0 : j] = con_zero.Process((float)data_correlation_source[i]);
                    continue;
                }

                for (int j = 0; j < _zero_correlator.Length; j++)
                {
                    int k = i - kerneldelay_zero;
                    if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.SSAD)
                    {
                        if (reverse_correlators)
                            zero_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_zero_correlator[(_zero_correlator.Length - 1) - j] - data_correlation_source[i + j], 2);
                        else
                            zero_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_zero_correlator[j] - data_correlation_source[i + j], 2);
                    }
                    else if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.MAC)
                    {
                        if (reverse_correlators)
                            zero_correlation[k < 0 ? 0 : k] += (_zero_correlator[(_zero_correlator.Length - 1) - j] * data_correlation_source[i + j]);
                        else
                            zero_correlation[k < 0 ? 0 : k] += (_zero_correlator[j] * data_correlation_source[i + j]);
                    }
                    else if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.SSAD_MAC)
                    {
                        double current_corr_value = 0;

                        if (reverse_correlators)
                            current_corr_value = _zero_correlator[(_one_correlator.Length - 1) - j];
                        else
                            current_corr_value = _zero_correlator[j];

                        if (current_corr_value < 0.01 && current_corr_value > 0.01)
                            zero_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow((current_corr_value - data_correlation_source[i + j]),2);
                        else
                            zero_correlation[k < 0 ? 0 : k] += (current_corr_value * data_correlation_source[i + j]);


                    }
                }

                if (i > kerneldelay_zero)
                    zero_correlation_sum += zero_correlation[i - kerneldelay_zero];
                zero_correlation_min = Math.Min(zero_correlation[i], zero_correlation_min);

            }
            zero_correlation_sum /= zero_correlation.Length;

            // correct start values
            if (kerneldelay_zero > 0)
            {
                zero_correlation[0] = zero_correlation[1];
            }
            else if (kerneldelay_zero < 0)
            {
                for (int i = kerneldelay_zero; i < 0; i++)
                {
                    zero_correlation[i - kerneldelay_zero] = zero_correlation[-kerneldelay_zero];
                }
            }

            one_correlation = new double[data_correlation_source.Length];
            double one_correlation_sum = 0;
            double one_correlation_min = double.PositiveInfinity;

            // correlation for one-bits
            for (int i = 0; i < data_correlation_source.Length - _one_correlator.Length; i++)
            {
                if (currentdemodulator.IsConvolver() && con_one != null)
                {
                    int j = i - kerneldelay_one;
                    one_correlation[j < 0 ? 0 : j] = con_one.Process((float)data_correlation_source[i]);
                    continue;
                }

                for (int j = 0; j < _one_correlator.Length; j++)
                {
                    int k = i - kerneldelay_one;
                    if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.SSAD)
                    {
                        if (reverse_correlators)
                            one_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_one_correlator[(_one_correlator.Length - 1) - j] - data_correlation_source[i + j], 2);
                        else
                            one_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_one_correlator[j] - data_correlation_source[i + j], 2);
                    }
                    else if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.MAC)
                    {
                        if (reverse_correlators)
                            one_correlation[k < 0 ? 0 : k] += (_one_correlator[(_one_correlator.Length - 1) - j] * data_correlation_source[i + j]);
                        else
                            one_correlation[k < 0 ? 0 : k] += (_one_correlator[j] * data_correlation_source[i + j]);
                    }
                    else if (currentdemodulator.CorrelatorParameters.CorrelatorMethod == DemodulatorContext.CorrelatorMethodEnum.SSAD_MAC)
                    {
                        double current_corr_value = 0;

                        if (reverse_correlators)
                            current_corr_value = _one_correlator[(_one_correlator.Length - 1) - j];
                        else
                            current_corr_value = _one_correlator[j];

                        if (current_corr_value < 0.01 && current_corr_value > 0.01)
                            one_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow((current_corr_value - data_correlation_source[i + j]), 2);
                        else
                            one_correlation[k < 0 ? 0 : k] += (current_corr_value * data_correlation_source[i + j]);


                    }
                }

                if (i > kerneldelay_one)
                    one_correlation_sum += one_correlation[i - kerneldelay_one];
                one_correlation_min = Math.Min(one_correlation[i], one_correlation_min);
            }

            one_correlation_sum /= one_correlation.Length;

            // correct start values
            if (kerneldelay_one > 0)
            {
                one_correlation[0] = one_correlation[1];
            }
            else if (kerneldelay_one < 0)
            {
                for (int i = kerneldelay_one; i < 0; i++)
                {
                    one_correlation[i - kerneldelay_one] = one_correlation[-kerneldelay_one];
                }
            }

            if (currentdemodulator.CorrelatorParameters.UseAverageSubtraction)
            {
                // offset correct the correlators
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] -= zero_correlation_sum;
                    one_correlation[i] -= one_correlation_sum;
                }
            }
            if (currentdemodulator.CorrelatorParameters.UseOutputHighPassFiltering)
            {
                NWaves.Filters.DcRemovalFilter one_hpf = new DcRemovalFilter(currentdemodulator.CorrelatorParameters.OutputHighPassFilterCoefficient);
                NWaves.Filters.DcRemovalFilter zero_hpf = new DcRemovalFilter(currentdemodulator.CorrelatorParameters.OutputHighPassFilterCoefficient);

                // run in the HPFs
                for (int i = 0; i < 50; i++)
                {
                    one_hpf.Process((float)one_correlation[0]);
                    zero_hpf.Process((float)zero_correlation[0]);
                }

                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] = zero_hpf.Process((float)zero_correlation[i]);
                    one_correlation[i] = one_hpf.Process((float)one_correlation[i]);
                }

            }

            currentdemodulator.CorrelatorParameters.OneDemodulatorResult = one_correlation;
            currentdemodulator.CorrelatorParameters.ZeroDemodulatorResult = zero_correlation;

            return;
        }

        /* Generate correlators, input is FM data to sample, and the UTC 0 ms position of the two waveforms */
        /*
        private static void Generate_Correlators(double[] correlator_template_source, DemodulatorContext.CorrelatorTypeEnum _type, int zero_offset, int one_offset)
        {
            StringBuilder correlation_output = new StringBuilder();
            double average_value = 0;
            double max_value = double.NegativeInfinity;
            double min_value = double.PositiveInfinity;
            // generate correlation templates, these are valid for the first websdr file (with fantastic SNR)
            int correlator1_template_offset = zero_offset;

            for (int i = correlator1_template_offset - 28; i < correlator1_template_offset + 28; i++)
            {
                if (correlator_template_source[i] > max_value)
                    max_value = correlator_template_source[i];
                if (correlator_template_source[i] < min_value)
                    min_value = correlator_template_source[i];
                average_value += correlator_template_source[i];
            }

            double template_scale = max_value - min_value;

            for (int i = correlator1_template_offset - 28; i < correlator1_template_offset + 28; i++)
            {
                correlation_output.AppendFormat("{0},", (correlator_template_source[i] - (average_value / 56)) / template_scale);
            }

            File.WriteAllText(String.Format("correlation_zero_{0}.txt", _type.GetString()), correlation_output.ToString());

            correlation_output.Clear();
            int correlator2_template_offset = one_offset;

            average_value = 0;
            max_value = double.NegativeInfinity;
            min_value = double.PositiveInfinity;
            for (int i = correlator1_template_offset - 25; i < correlator1_template_offset + 43; i++)
            {
                if (correlator_template_source[i] > max_value)
                    max_value = correlator_template_source[i];
                if (correlator_template_source[i] < min_value)
                    min_value = correlator_template_source[i];
                average_value += correlator_template_source[i];
            }

            template_scale = max_value - min_value;

            // generate correlation template
            for (int i = correlator2_template_offset - 25; i < correlator2_template_offset + 43; i++)
            {
                correlation_output.AppendFormat("{0},", (correlator_template_source[i] - (average_value / 56)) / template_scale);
            }

            File.WriteAllText(String.Format("correlation_one_{0}.txt", _type.GetString()), correlation_output.ToString());

            correlation_output.Clear();

            // generate correlation template
            for (int i = 1475; i < 1700; i++)
            {
                correlation_output.AppendFormat("{0},", 0);// fm_filtered_rectified[i]);
            }

            File.WriteAllText(String.Format("correlation_minute_{0}.txt", _type.GetString()), correlation_output.ToString());
        }
        */

        public static void Generate_Synthetic_Correlators(ref double[] FM_One, ref double[] FM_Zero, ref double[] PM_One, ref double[] PM_Zero, int moving_average_length = 0, int moving_average_length_pm = 0)
        {
            // generate synthetic correlators with "perfect" frequency response
            int samplerate = 200;
            double sampletime = 1 / (double)samplerate; // 5 ms
            double template_length_s = 0.3;

            // filter used for processing
            NWaves.Filters.MovingAverageFilter fm_lpf = new NWaves.Filters.MovingAverageFilter(moving_average_length);

            // store it in a list for convenience
            List<double> tempdata = new List<double>((int)(template_length_s / sampletime));

            /* the structure of a FM zero is
             * (20) 100 ms of 0 (pre-time)
             * (5) +1 for 25 ms
             * (10) -1 for 50 ms (middle of this negative pulse is the UTC mark)
             * (10) +1 for 25 ms
             * (25) 0 for 125 ms
             * 
             * total length = 300 ms
             */

            // generate FM zero
            // pre-time
            for (int i = 0; i < 15; i++)
            {
                tempdata.Add(0);
            }
            // +1
            for (int i = 0; i < 5; i++)
            {
                tempdata.Add(+1);
            }
            // -1
            for (int i = 0; i < 10; i++)
            {
                tempdata.Add(-1);
            }
            // +1
            for (int i = 0; i < 5; i++)
            {
                tempdata.Add(+1);
            }
            // 0
            for (int i = 0; i < 20; i++)
            {
                tempdata.Add(0);
            }

            FM_Zero = tempdata.ToArray();

            if (moving_average_length > 0)
            {
                for (int i = 0; i < FM_Zero.Length; i++)
                    FM_Zero[i] = fm_lpf.Process((float)FM_Zero[i]);
            }

            // generate PM by integration
            double integration_gain = 0.2;
            double integrator = 0;
            for (int i = 0; i < tempdata.Count; i++)
            {
                integrator += tempdata[i] * integration_gain;
                PM_Zero[i] = integrator;
            }


            if (moving_average_length_pm > 0)
            {
                fm_lpf.Reset();
                for (int i = 0; i < PM_Zero.Length; i++)
                    PM_Zero[i] = fm_lpf.Process((float)PM_Zero[i]);
            }


            fm_lpf.Reset();
            tempdata.Clear();

            /* the structure of a FM one is
             * 20 100 ms of 0 (pre-time)
             * 5 +1 for 25 ms
             * 10 -1 for 50 ms
             * 10 +1 for 50 ms
             * 10 -1 for 50 ms
             * 5 +1 for 25 ms
             * 5 0 for 25 ms?
             * 
             * total length = 300 ms
             */

            // generating one FM

            // pre-time 15
            for (int i = 0; i < 15; i++)
            {
                tempdata.Add(0);
            }
            // +1 5
            for (int i = 0; i < 5; i++)
            {
                tempdata.Add(+1);
            }
            // -1 10
            for (int i = 0; i < 10; i++)
            {
                tempdata.Add(-1);
            }
            // +1 10
            for (int i = 0; i < 10; i++)
            {
                tempdata.Add(+1);
            }
            // -1 10
            for (int i = 0; i < 10; i++)
            {
                tempdata.Add(-1);
            }
            // +1 5
            for (int i = 0; i < 5; i++)
            {
                tempdata.Add(1);
            }
            // add some nulls
            //tempdata.Add(0);

            /*// 0 5
            for (int i = 0; i < 10; i++)
            {
                tempdata.Add(0);
            }*/

            FM_One = tempdata.ToArray();

            if (moving_average_length > 0)
            {
                for (int i = 0; i < FM_One.Length; i++)
                    FM_One[i] = fm_lpf.Process((float)FM_One[i]);
            }

            integrator = 0;
            for (int i = 0; i < tempdata.Count; i++)
            {
                integrator += tempdata[i] * integration_gain;
                PM_One[i] = integrator;
            }

            if (moving_average_length_pm > 0)
            {
                fm_lpf.Reset();
                for (int i = 0; i < PM_One.Length; i++)
                    PM_One[i] = fm_lpf.Process((float)PM_One[i]);
            }
        }


    }
}
