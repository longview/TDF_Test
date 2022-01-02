using NWaves.Filters;
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


        static void Main(string[] args)
        {

            Modes mode = Modes.Verify;
            //Modes mode = Modes.Standard;
            int testindex = 0;

            DemodulatorContext currentdemodulator = GenerateDemodulator(DemodulatorDefaults.FM_Biased_MeanVariance);

            List<TestSignalInfo> testsignals = new List<TestSignalInfo>();
            // input file should be mono 16-bit, 20000 Hz (oddball rate)
            // we can resample, but it's not very fast
            // WARNING: any non-20 kHz file will be overwritten in place with a resampled version
            PopulateTestSignals(testsignals);

            TestSignalInfo testsignal_current = testsignals[testindex];

            if (mode == Modes.Standard)
            {
                Console.WriteLine("Test start at time {0}\r\n", DateTime.UtcNow.ToString("o"));
                Console.WriteLine("Using test index {0}, signal type {7}.\r\nFile {1} (IF = {6})\r\nSNR {2}, station was {3}.\r\nTime transmitted: {4}.\r\nComment: {5}",
                testindex, testsignal_current.FilePath, testsignal_current.SNR,
                testsignal_current.Status == TestSignalInfo.Station_Status.OnAir ? "on air" : "off air",
                testsignal_current.RecordedTimestampUTC.ToString("o"), testsignal_current.Comment, testsignal_current.Frequency,
                testsignal_current.SignalType == TestSignalInfo.Signal_Type.TDF ? "TDF" : "DCF77 Phase");
            }


            Console.WriteLine("Using {0} correlation", currentdemodulator.ToString());

            // generate correlators if desired
            // this code path is dead for now, needs a slight rewrite to use the demodulatorcontext
            if (false)
            {
#pragma warning disable CS0162 // Unreachable code detected
                StringBuilder console_output = new StringBuilder();
#pragma warning restore CS0162 // Unreachable code detected
                int zero = 1725;
                int one = 2926;
                //Demodulate_Testsignal(testsignals[0], CorrelatorType.PM, ref console_output, true, zero, one);
                //Demodulate_Testsignal(testsignals[0], CorrelatorType.FM, ref console_output, true, zero, one);
                Console.WriteLine("Generated correlators, offset {0} (0) and {1} (1)", zero, one);
            }

            double[] sweep_results = new double[20];

            if (mode == Modes.Verify)
            {
                int fail_count = 0;
                int antifail_count = 0;

                foreach (TestSignalInfo signal in testsignals)
                {
                    int errors = 0;
                    StringBuilder console_output = new StringBuilder();
                    console_output.AppendFormat("Test start at time {0}\r\n", DateTime.UtcNow.ToString("o"));
                    console_output.AppendFormat("Using test index {0}, signal type {7}.\r\nFile {1} (IF = {6})\r\nSNR {2}, station was {3}.\r\nTime transmitted: {4}.\r\nComment: {5}\r\n\r\n",
                    testsignals.IndexOf(signal), signal.FilePath, signal.SNR,
                    signal.Status == TestSignalInfo.Station_Status.OnAir ? "on air" : "off air",
                    signal.RecordedTimestampUTC.ToString("o"), signal.Comment, signal.Frequency,
                    signal.SignalType == TestSignalInfo.Signal_Type.TDF ? "TDF" : "DCF77 Phase");

                    errors = Demodulate_Testsignal(signal, ref currentdemodulator, ref console_output);

                    // ignore the bad signals for error computation
                    if (signal.Status == TestSignalInfo.Station_Status.OnAir)
                    {
                        if (errors > signal.ExpectedErrors)
                            fail_count++;
                        else if (errors < signal.ExpectedErrors)
                            antifail_count++;
                    }

                    console_output.AppendLine();
                    console_output.Append(currentdemodulator.ToLongString());

                    Console.WriteLine("Index {0,2}, expected errors {1,2}, found {2,2}{4}. Comment: {3}",
                        testsignals.IndexOf(signal), signal.ExpectedErrors, errors, signal.Comment, errors < signal.ExpectedErrors ? "(!)" : "");

                    File.WriteAllText(String.Format("Verify_Result_{0}_f{1}_e{2}_{3}.txt", testsignals.IndexOf(signal), errors, signal.ExpectedErrors, currentdemodulator.ToString()),
                        console_output.ToString());
                }

                Console.WriteLine("Finished tests, {0} failures of {1} total{2}", fail_count, testsignals.Count, antifail_count > 0 ? String.Format(". {0} test(s) exceeded specification!", antifail_count) : ".");
            }
            else
            {
                StringBuilder console_output = new StringBuilder();

                // self test of timecode generator
                //Decode_Received_Data(testsignal_current, testsignal_current.reference_timecode.GetBitstream(), ref console_output);
                //Console.Write(console_output.ToString());

                Demodulate_Testsignal(testsignal_current, ref currentdemodulator, ref console_output);

                Console.Write(console_output.ToString());
            }

            Console.ReadLine();

        }

        private static void PopulateTestSignals(List<TestSignalInfo> testsignals)
        {

            //0 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\websdr_recording_start_2021-12-28T12_57_51Z_157.0kHz.wav", "webSDR recording, high quality",
                70, new DateTime(2021, 12, 28, 12, 57, 51, DateTimeKind.Utc)));
            //1 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T163350Z, 157 kHz, Wide-U.wav", "Ok signal, mid day",
                30, new DateTime(2021, 12, 29, 16, 33, 50, DateTimeKind.Utc)));
            //2 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T185106Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                40, new DateTime(2021, 12, 29, 18, 51, 06, DateTimeKind.Utc)));
            //3 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T090027Z, 157 kHz, Wide-U.wav", "Medium signal, morning",
                24, new DateTime(2021, 12, 30, 09, 00, 27, DateTimeKind.Utc)));
            //4 full of errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T102229Z, 157 kHz, Wide-U.wav", "Maintenance phase, off air",
                0, new DateTime(2021, 12, 30, 10, 22, 29, DateTimeKind.Utc), _errors: 25, _status: TestSignalInfo.Station_Status.Maintenance));
            //5 no errors, bit 19/20 is tricky
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T105034Z, 157 kHz, Wide-U.wav", "Medium signal, morning",
                24, new DateTime(2021, 12, 30, 10, 50, 34, DateTimeKind.Utc)));
            //6 decodes with 5 errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T121742Z, 157 kHz, Wide-U_20.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 12, 17, 42, DateTimeKind.Utc), _errors: 10));
            //7 
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T121914Z, 157 kHz, Wide-U_20.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 12, 19, 14, DateTimeKind.Utc), _errors: 5));
            //8 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T142316Z, 157 kHz, Wide-U.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 14, 23, 16, DateTimeKind.Utc)));
            //9 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T172433Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                29, new DateTime(2021, 12, 30, 17, 24, 33, DateTimeKind.Utc)));
            //10 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T181314Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                30, new DateTime(2021, 12, 30, 18, 13, 14, DateTimeKind.Utc)));
            // 11
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T200920Z, 157 kHz, Wide-U.wav", "Excellent signal, evening",
                43, new DateTime(2021, 12, 30, 20, 09, 20, DateTimeKind.Utc)));
            // 12
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T235552Z, 157 kHz, Wide-U.wav", "Excellent signal, night",
                48, new DateTime(2021, 12, 30, 23, 55, 52, DateTimeKind.Utc), holidaytomorrow: true));
            // 13 - tricky start, minute is around 7000
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T181322Z, 157 kHz, Wide-U.wav", "Poor signal, evening",
                22, new DateTime(2021, 12, 31, 18, 13, 22, DateTimeKind.Utc), _errors: 35, holidaytomorrow: true));
            // 14 - minute start around 6500
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T181524Z, 157 kHz, Wide-U.wav", "Poor signal, evening",
                22, new DateTime(2021, 12, 31, 18, 15, 24, DateTimeKind.Utc), _errors: 35, holidaytomorrow: true));
            // 15
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T222827Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                20, new DateTime(2021, 12, 31, 22, 28, 27, DateTimeKind.Utc), holidaytomorrow: true));
            // 16 - last of the year :)
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T225740Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 57, 40, DateTimeKind.Utc), _errors: 7, holidaytomorrow: true));
            // 17 - first of the year
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T225835Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 58, 35, DateTimeKind.Utc), _errors: 2, holidaytoday: true));
            // 18
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-31T225930Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 59, 30, DateTimeKind.Utc), holidaytoday: true));
            // 19
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T110116Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                15, new DateTime(2022, 01, 02, 11, 01, 20, DateTimeKind.Utc), _errors: 25));
            // 20
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T115821Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                18, new DateTime(2022, 01, 02, 11, 58, 22, DateTimeKind.Utc)));
            // 21
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T130333Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                16, new DateTime(2022, 01, 02, 13, 03, 33, DateTimeKind.Utc), _errors: 22));
            // 22
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T155905Z, 157 kHz, Wide-U.wav", "Good signal, afternoon day",
                34, new DateTime(2022, 01, 02, 15, 59, 05, DateTimeKind.Utc)));
        }

        public enum Modes
        {
            Standard,
            Verify
        }


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



        private static int Demodulate_Testsignal(TestSignalInfo testsignal_current, ref DemodulatorContext demodulator,
            ref StringBuilder console_output,
            bool generate_correlator = false)
        {
            // frequency offset of USB receiver
            double frequency = testsignal_current.Frequency;

            // TODO: add some kind of struct with e.g. filter-coefficients (perhaps the filter objects themselves?)
            // to make it easier to tweak parameters at the top level
            double samplerate = 20000;
            double sampleperiod = 1 / samplerate;

            double[] data;
            //double[] data_right;
            //openWav(testsignal_current.FilePath, out data, out data_right);

            NWaves.Audio.WaveFile inputsignal;

            using (FileStream wavefilestream = new FileStream(testsignal_current.FilePath, FileMode.Open))
            {
                // open the wave file (normalized)
                inputsignal = new NWaves.Audio.WaveFile(wavefilestream);
            }

            NWaves.Signals.DiscreteSignal insignal = inputsignal.Signals[0];

            if (inputsignal.WaveFmt.SamplingRate != samplerate)
            {
                //throw new InvalidDataException(String.Format("Input wave file {0} has sample rate {1}, must be {2}", testsignal_current.FilePath, inputsignal.WaveFmt.SamplingRate, samplerate));
                // do a conversion
                var resampler = new NWaves.Operations.Resampler();
                insignal = resampler.Resample(inputsignal.Signals[0], (int)samplerate);
                console_output.AppendFormat("Note: source file was resampled to {0} from {1}.\r\n", (int)samplerate, inputsignal.WaveFmt.SamplingRate);
                using (FileStream wavefilestream = new FileStream(testsignal_current.FilePath, FileMode.OpenOrCreate))
                {
                    wavefilestream.SetLength(0);
                    // open the wave file (normalized)
                    var wavefile = new NWaves.Audio.WaveFile(insignal);
                    wavefile.SaveTo(wavefilestream);
                }
                console_output.AppendFormat("And the file was overwritten with the resampled version!\r\n");
            }

            if (inputsignal.WaveFmt.ChannelCount > 1)
                console_output.AppendFormat("Note: source file has {0} channels, only the left will be used.\r\n", inputsignal.WaveFmt.ChannelCount);

            data = new double[insignal.Length];
            List<double> data_list = new List<double>(insignal.Length);
            foreach (float f in insignal.Samples)
                data_list.Add(f);

            data = data_list.ToArray();

            // TODO: this is wrong, fix it
            double phase_error_per_sample_vs_frequency = (0.6035457 + -0.1047503) / 10;

            console_output.AppendFormat("Read file, length {0} samples, {1} seconds\r\n", data.Length, data.Length / samplerate);

            // debug time register to allow plotting in time
            double[] timescale = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                timescale[i] = i * sampleperiod;
            }

            int IQ_decimation_factor = 100;
            double decimated_sampleperiod = IQ_decimation_factor * sampleperiod;
            demodulator.DecimatedSamplePeriod = decimated_sampleperiod;

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

            console_output.AppendFormat("Using sample rate {0}, output decimation {1}, IQ conversion, LO {2}\r\n", samplerate, IQ_decimation_factor, frequency);
            Perform_Downconversion(frequency, samplerate, data, IQ_decimation_factor, i_unfiltered, q_unfiltered, i_filtered, q_filtered, ref console_output);

            console_output.AppendFormat("FM demodulation start\r\n");

            console_output.AppendFormat("FM moving average filter size {0}\r\nFM rectifier filter size {1}\r\n", 
                demodulator.FilterParameters.FMAverageCount, demodulator.FilterParameters.EnvelopeAverageCount);
            double fm_unfiltered_square, fm_filtered_square;
            Demodulate(i_filtered, q_filtered, fm_unfiltered, pm_unfiltered, fm_filtered, demodulator.FilterParameters.FMAverageCount, out fm_unfiltered_square, out fm_filtered_square);
            Perform_PM_Correction(phase_error_per_sample_vs_frequency, ref pm_unfiltered, pm_filtered_drift, ref console_output);

            // obsolete now
            /*if (generate_correlator && (_correlatortype == CorrelatorType.FM || _correlatortype == CorrelatorType.FM_Convolve))
                Generate_Correlators(fm_filtered, _correlatortype, zero_offset, one_offset);
            else if (generate_correlator && _correlatortype == CorrelatorType.PM)
                Generate_Correlators(pm_filtered_drift, _correlatortype, zero_offset, one_offset);*/

            // attempt an SNR calculation based on full band (signal and noise)
            // and filtered square values (mostly just signal we assume)

            FM_SNR_Calculation(fm_unfiltered, fm_filtered, ref fm_unfiltered_square, ref fm_filtered_square, ref console_output);

            double[] fm_filtered_rectified = Generate_Rectified_FM(data, IQ_decimation_factor, fm_filtered, demodulator.FilterParameters.EnvelopeAverageCount);

            // make synthetic correlators if desired
            if (demodulator.CorrelatorParameters.CorrelatorReferenceSource == DemodulatorContext.CorrelatorReferenceSourceTypes.Synthetic)
            {
                Generate_Synthetic_Correlators(ref one_correlator_template_FM, ref zero_correlator_template_FM, ref one_correlator_template_PM, ref zero_correlator_template_PM, 
                    demodulator.CorrelatorParameters.SyntheticCorrelatorAverageCount, demodulator.CorrelatorParameters.SyntheticCorrelatorAverageCount);
            }

            if (demodulator.CorrelatorParameters.CorrelatorDataSource == DemodulatorContext.CorrelatorDataSourceTypes.FM)
            {
                demodulator.CorrelatorParameters.ZeroCorrelatorReference = zero_correlator_template_FM;
                demodulator.CorrelatorParameters.OneCorrelatorReference = one_correlator_template_FM;
                demodulator.CorrelatorParameters.DemodulatorSource = fm_filtered;
            }
            else
            {
                demodulator.CorrelatorParameters.ZeroCorrelatorReference = zero_correlator_template_PM;
                demodulator.CorrelatorParameters.OneCorrelatorReference = one_correlator_template_PM;
                demodulator.CorrelatorParameters.DemodulatorSource = pm_filtered_drift;
            }

            demodulator.MinuteDetectorParameters.MinuteDetectorSource = fm_filtered_rectified;

            Perform_Correlations(ref demodulator, ref console_output);

            Find_Minute_Start(ref demodulator, testsignal_current, ref console_output);

            Calculate_Signal_SNR(fm_filtered, fm_filtered_square, demodulator.MinuteDetectorParameters.MinuteDetectorResult, ref console_output);

            Perform_Detection(ref demodulator, testsignal_current, ref console_output);


            console_output.Append("Decode: ");
            Print_Demodulated_Bits(demodulator.DemodulationResult.DemodulatedData, ref console_output);
            console_output.Append("Refrnc: ");
            Print_Demodulated_Bits(testsignal_current.Reference_Timecode.GetBitstream(), ref console_output);

            demodulator.DemodulationResult.BitErrors = testsignal_current.Reference_Timecode.CompareBitstream(demodulator.DemodulationResult.DemodulatedData);
            demodulator.DemodulationResult.DemodulatedDataErrorMask = testsignal_current.Reference_Timecode.GetBitstreamErrorMask();
            demodulator.DemodulationResult.DemodulatedDataReference = testsignal_current.Reference_Timecode.GetBitstream();
            demodulator.DemodulationResult.DemodulatedDataErrorDescription = testsignal_current.Reference_Timecode.Comparison_Error_Description;

            Print_Demodulated_Bits_Informative(console_output, ref demodulator);

            console_output.Append(demodulator.DemodulationResult.DemodulatedDataErrorDescription);

            demodulator.DemodulationResult.DecodeErrors = Decode_Received_Data(testsignal_current, demodulator.DemodulationResult.DemodulatedData, ref console_output);

            return demodulator.DemodulationResult.BitErrors + demodulator.DemodulationResult.DecodeErrors;
        }

        private static void Print_Demodulated_Bits_Informative(StringBuilder console_output, ref DemodulatorContext currentdemodulator)
        {
            bool[] payload_data = currentdemodulator.DemodulationResult.DemodulatedData;
            bool[] reference_data = currentdemodulator.DemodulationResult.DemodulatedDataReference;
            double[] second_sampling_ratio = currentdemodulator.DataSlicerResults.SecondSampleRatios;
            double[] second_sampling_margin = currentdemodulator.DataSlicerResults.RatioVsThreshold;
            // print out informative data to aid debugging:
            int count = 0;
            console_output.AppendFormat("No.  Sym  Value   Expct    Rat      Margin\r\n");
            console_output.AppendFormat("00   M    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("01   A2   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("02   A3   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("03  HA02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("04  HA04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("05  HA08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("06  HA16  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("07   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("08   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("09   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("10   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("11   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("12   0    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("13   F1   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("14   F2   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("15   N/A  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("16   A1   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("17   Z1   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("18   Z2   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("19   X    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("20   S    {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("21   M01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("22   M02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("23   M04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("24   M08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("25   M10  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("26   M20  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("27   M40  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("28   P1   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("29   H01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("30   H02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("31   H04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("32   H08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("33   H10  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("34   H20  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("35   P2   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("36  DM01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("37  DM02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("38  DM04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("39  DM08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("40  DM10  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("41  DM20  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("42  DW01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("43  DW02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("44  DW04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("45  MO01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("46  MO02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("47  MO04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("48  MO08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("49  MO10  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("50   Y01  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("51   Y02  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("52   Y04  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("53   Y08  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("54   Y10  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("55   Y20  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("56   Y40  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("57   Y80  {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            count++;
            console_output.AppendFormat("58   P3   {0,5}   {2,5}{3}   {1:0.0000;-0.000}   {4:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count], reference_data[count], (payload_data[count] ^ reference_data[count]) ? "*" : " ", second_sampling_margin[count]);
            console_output.AppendLine();
        }

        private static int Decode_Received_Data(TestSignalInfo testsignal_current, bool[] payload_data, ref StringBuilder console_output)
        {
            int decode_error_count = 0;
            // let's parse the data!
            console_output.AppendFormat(payload_data[0] ? "M: First bit error\r\n" : "M: First bit ok\r\n");
            if (payload_data[0])
                decode_error_count++;

            console_output.AppendFormat(payload_data[1] ? "A2: Positive Leap Warning\r\n" : "A2: No Pos Leap\r\n");
            console_output.AppendFormat(payload_data[2] ? "A3: Negative Leap Warning\r\n" : "A3: No Neg Leap\r\n");

            int hammingweight = (payload_data[3] ? 2 : 0) + (payload_data[4] ? 4 : 0) + (payload_data[5] ? 8 : 0) + (payload_data[6] ? 16 : 0);
            int hammingcount = 0;
            for (int i = 21; i < 59; i++)
            {
                if (payload_data[i])
                    hammingcount++;
            }

            console_output.AppendFormat("Hamming weight 21-58 is {0}, I count {1}, this is {2}\r\n", hammingweight, hammingcount, hammingcount == hammingweight ? "good!" : "bad :(");
            if (hammingcount != hammingweight)
            {
                decode_error_count++;
                if ((hammingcount - hammingweight) % 2 == 0)
                {
                    console_output.AppendFormat("Hamming weight error is even; this means parity errors may not be detected.\r\n");
                }
            }


            if (!payload_data[7] && !payload_data[8] && !payload_data[9] && !payload_data[10] && !payload_data[11] && !payload_data[12])
            {
                console_output.AppendFormat("Unused bits 7-12 ok!\r\n");
            }
            else
            {
                // count true bits
                for (int i = 7; i < 13; i++)
                    decode_error_count += payload_data[i] ? 1 : 0;

                console_output.AppendFormat("Unused bits 7-12 error!\r\n");
            }


            console_output.AppendFormat(payload_data[13] ? "F1: Tomorrow is a public holiday!\r\n" : "F1: No holiday tomorrow\r\n");
            console_output.AppendFormat(payload_data[14] ? "F2: Today is a public holiday!\r\n" : "F2: No holiday today :(\r\n");
            console_output.AppendFormat(payload_data[15] ? "Bit 15 is high, ignored\r\n" : "Bit 15 is low, ignored\r\n");
            console_output.AppendFormat(payload_data[16] ? "A1: Time zone will change at the next hour mark.\r\n" : "A1: Time zone will not change at the next hour mark\r\n");
            console_output.AppendFormat(payload_data[17] ? "Z1: Currently using CEST\r\n" : "Z2: Not using CEST\r\n");
            console_output.AppendFormat(payload_data[18] ? "Z2: Currently using CET\r\n" : "Z2: Not using CET\r\n");

            // No spec, but these seem like they should never happen?
            if (payload_data[17] && payload_data[18])
            {
                decode_error_count++;
                console_output.AppendFormat("Error: Using both CET and CEST, how?\r\n");
            }
            else if (!payload_data[17] && !payload_data[18])
            {
                decode_error_count++;
                console_output.AppendFormat("Error: Not using CET, nor CEST, how?\r\n");
            }

            // for the C# version we known about the timezone in use, so we just set it
            TimeZoneInfo decoded_tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");


            console_output.AppendFormat(payload_data[19] ? "Unused bit 19 is high, error\r\n" : "Unused bit 19 ok\r\n");
            if (payload_data[19])
                decode_error_count++;
            console_output.AppendFormat(payload_data[20] ? "S: Start of time ok\r\n" : "S: Start of time framing error\r\n");
            if (!payload_data[20])
                decode_error_count++;

            // BCD format, watch out
            // we need to watch it with parity calculations since the number of set bits could be different before/after conversion to normal binary?
            int minutes = (payload_data[21] ? 1 : 0) + (payload_data[22] ? 2 : 0) + (payload_data[23] ? 4 : 0) + (payload_data[24] ? 8 : 0) + (payload_data[25] ? 10 : 0) +
                (payload_data[26] ? 20 : 0) + (payload_data[27] ? 40 : 0);

            int paritycount = 0;
            for (int i = 21; i < 28; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            console_output.AppendFormat(paritycount % 2 == 1 == payload_data[28] ? "P1: Minute parity ok\r\n" : "P1: Minute parity error\r\n");

            if (paritycount % 2 == 1 != payload_data[28])
                decode_error_count++;




            int hours = (payload_data[29] ? 1 : 0) + (payload_data[30] ? 2 : 0) + (payload_data[31] ? 4 : 0) + (payload_data[32] ? 8 : 0) + (payload_data[33] ? 10 : 0) +
                (payload_data[34] ? 20 : 0);

            paritycount = 0;
            for (int i = 29; i < 35; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            console_output.AppendFormat(paritycount % 2 == 1 == payload_data[35] ? "P2: Hours parity ok\r\n" : "P2: Hours parity error\r\n");
            if (paritycount % 2 == 1 != payload_data[35])
                decode_error_count++;

            int day_of_month = (payload_data[36] ? 1 : 0) + (payload_data[37] ? 2 : 0) + (payload_data[38] ? 4 : 0) + (payload_data[39] ? 8 : 0) + (payload_data[40] ? 10 : 0) +
                (payload_data[41] ? 20 : 0);
            int day_of_week = (payload_data[42] ? 1 : 0) + (payload_data[43] ? 2 : 0) + (payload_data[44] ? 4 : 0);
            int month = (payload_data[45] ? 1 : 0) + (payload_data[46] ? 2 : 0) + (payload_data[47] ? 4 : 0) + (payload_data[48] ? 8 : 0) + (payload_data[49] ? 10 : 0);
            int year = (payload_data[50] ? 1 : 0) + (payload_data[51] ? 2 : 0) + (payload_data[52] ? 4 : 0) + (payload_data[53] ? 8 : 0) + (payload_data[54] ? 10 : 0) +
                (payload_data[55] ? 20 : 0) + (payload_data[56] ? 40 : 0) + (payload_data[57] ? 80 : 0);


            if (day_of_week > 7 || day_of_week < 1)
            {
                console_output.AppendFormat("Day of week {0} is outside of allowable range (1-7)\r\n", day_of_week);
                decode_error_count++;
            }

            if (day_of_month > 31 || day_of_month < 1)
            {
                console_output.AppendFormat("Day of month {0} is outside of allowable range (1-31)\r\n", year);
                decode_error_count++;
            }

            if (month > 12 || month < 1)
            {
                console_output.AppendFormat("Month {0} is outside of allowable range (1-12)\r\n", month);
                decode_error_count++;
            }

            if (year > 99)
            {
                console_output.AppendFormat("Year {0} is outside of allowable range (0-99)\r\n", year);
                decode_error_count++;
            }

            paritycount = 0;
            for (int i = 36; i < 58; i++)
            {
                if (payload_data[i])
                    paritycount++;
            }

            console_output.AppendFormat(paritycount % 2 == 1 == payload_data[58] ? "P3: Date bits parity ok\r\n" : "P3: Date bits parity error\r\n");
            if (paritycount % 2 == 1 != payload_data[58])
                decode_error_count++;

            console_output.AppendFormat("At the next minute marker: {0:D2}:{1:D2}, day of month {2}, day of week {3}, month {4}, year is {5:D4}\r\n",
                hours, minutes, day_of_month, day_of_week, month, year + 2000);

            // this conversion "knows" that we are in the same time zone as the transmitter; this is not guaranteed
            // should use the timezone info decoded above
            // put this in a try/catch since we don't know if the datetime object will be valid
            try
            {
                DateTime decoded_time = new DateTime(year + 2000, month, day_of_month, hours, minutes, 0, DateTimeKind.Local);
                DateTimeOffset decoded_offset_time = new DateTimeOffset(decoded_time, decoded_tz.GetUtcOffset(decoded_time));
                console_output.AppendFormat("Decoded time is valid: ");
                console_output.AppendFormat(decoded_offset_time.UtcDateTime.ToString("o"));
                console_output.AppendFormat(" and locally {0}\r\n", decoded_time.ToString("o"));

                int dow_corrected = (int)decoded_time.DayOfWeek;
                if (dow_corrected == 0)
                    dow_corrected = 7;
                if (dow_corrected != day_of_week)
                {
                    console_output.AppendFormat("Decoded day of week is wrong, {0} should be {1}\r\n", day_of_week, decoded_time.DayOfWeek);
                    decode_error_count++;
                }
                else
                {
                    console_output.AppendFormat("Decoded day of week seems correct ({0})\r\n", decoded_time.DayOfWeek.ToString());
                }

                // check if the time is equal to the test recording info
                if (testsignal_current.RecordedTimestampUTC.CompareTo(decoded_time.ToUniversalTime()) == 0)
                    console_output.AppendFormat("Decoded time matches recording timestamp.\r\n");
                else
                {
                    console_output.AppendFormat("Decoded time does not match timestamp ({0}).\r\n", testsignal_current.RecordedTimestampUTC.ToUniversalTime().ToString("o"));
                    TimeSpan decoded_time_error = testsignal_current.RecordedTimestampUTC - decoded_time.ToUniversalTime();
                    console_output.AppendFormat("Decoded time error is: {0} (D:HH:MM:SS,SS')\r\n", decoded_time_error.ToString("G"));
                    decode_error_count++;
                }
            }
            catch (Exception)
            {
                console_output.AppendFormat("Decoded date and time is not valid.\r\n");
                decode_error_count++;
            }
            console_output.AppendFormat("(Blind) Decode found {0} errors, SNR {1})\r\n", decode_error_count, testsignal_current.SNR);
            if (decode_error_count < testsignal_current.ExpectedErrors)
                console_output.AppendFormat("Error count ({0}) was better than specified!\r\n", decode_error_count);
            if (testsignal_current.Status == TestSignalInfo.Station_Status.Maintenance)
                console_output.AppendFormat("Station was known to be off air, errors are expected.\r\n");
            console_output.AppendFormat("Finished\r\n");
            return decode_error_count;
        }

        private static void Print_Demodulated_Bits(bool[] payload_data, ref StringBuilder console_output)
        {
            foreach (bool bit in payload_data)
            {
                console_output.AppendFormat("{0}", bit ? 1 : 0);
            }
            console_output.AppendLine();
        }

        private static void Perform_Detection(ref DemodulatorContext currentdemodulator, TestSignalInfo testsignal, ref StringBuilder console_output
            )
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
            double datasampler_second_pos_range  = currentdemodulator.DataSlicerParameters.SearchRange;

            int minutestart_sample = currentdemodulator.MinuteDetectorParameters.MinuteDetectorResult;

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
                double min_zero = double.PositiveInfinity;
                int max_zero_time = 0;
                int min_zero_time = 0;

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

                    if (zero_correlation[i] < min_zero)
                    {
                        min_zero = zero_correlation[i];
                        min_zero_time = i;
                    }
                }

                // store the value found for zero/one
                currentdemodulator.DataSlicerResults.ZeroPeaks[secondcount] = max_zero;
                currentdemodulator.DataSlicerResults.OnePeaks[secondcount] = max_one;


                // test to improve detection reliability
                // basically a 3-element correlation on the expected correlation waveform
                // this improves SNR for good signals
                // but also seems to make things rapidly go bad when SNR is low, so no good!
                if (currentdemodulator.DataSlicerParameters.UseFIROffset || currentdemodulator.DataSlicerParameters.UseSymmetryWeight)
                {
                    double zero_leading_valley = zero_correlation[max_zero_time - 10];
                    double zero_trailing_valley = zero_correlation[max_zero_time + 10];
                    double zero_symmetry = Math.Abs(zero_leading_valley - zero_trailing_valley) * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                    double zero_offset = max_zero + (zero_leading_valley + zero_trailing_valley) / 2;

                    double one_leading_valley = one_correlation[max_one_time + 10];
                    double one_trailing_valley = one_correlation[max_one_time - 10];
                    double one_symmetry = Math.Abs(one_leading_valley - one_trailing_valley);
                    double one_offset = max_one + (one_leading_valley + one_trailing_valley) / 2;

                    // symmetry check might improve performance?
                    if (currentdemodulator.DataSlicerParameters.UseSymmetryWeight)
                    {
                        max_zero -= zero_symmetry * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                        max_one -= one_symmetry * currentdemodulator.DataSlicerParameters.SymmetryWeightFactor;
                    }

                    if (currentdemodulator.DataSlicerParameters.UseFIROffset)
                    {
                        max_zero -= zero_offset * currentdemodulator.DataSlicerParameters.FIROffsetFactor;
                        max_one -= one_offset * currentdemodulator.DataSlicerParameters.FIROffsetFactor;
                    }

                    currentdemodulator.DataSlicerResults.OneWeightedPeaks[secondcount] = max_one;
                    currentdemodulator.DataSlicerResults.ZeroWeightedPeaks[secondcount] = max_zero;
                }

                // the first second we run is always a 0, assuming we detected the minute correctly
                // the one-detector tends to be more reliable since it's longer, so we try to fudge it a bit
                // we can't make the zero detector any longer while staying in spec
                // for a real-time system we can expect to know what most of the bits are already
                // so we could (very carefully) bias the detector to the expected value
                // obviously this must be done carefully since otherwise we will never update our expectations if something changes
                // we could perhaps do both (i.e. record both an actual reading + the expected), time bits are expected to change linearly, and status bits are expected to
                // be static every hour (and in most cases static for many hours). this can be used to average out errors in most cases.
                if (secondcount == 0)
                {
                    // correct based on first measurement
                    if (currentdemodulator.DataSlicerParameters.UseInitialZeroCorrection)
                        datasampler_bias_scale = max_zero / max_one;
                    // correct manually to make it work better in poor SNR
                    //datasampler_bias_scale *= 1 + datasampler_bias_scale_offset;
                    // correct for template length; slight layering violation
                    // this check only applies to standard correlation, not the convolvers
                    if (currentdemodulator.DataSlicerParameters.UseTemplateLengthCorrection)
                        datasampler_bias_scale *= (double)currentdemodulator.CorrelatorParameters.ZeroCorrelatorReference.Length / (double)currentdemodulator.CorrelatorParameters.OneCorrelatorReference.Length;
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

                datasampler_start = max_time + (int)(datasampler_second_neg_range / decimated_sampleperiod);
                datasampler_stop = max_time + (int)(datasampler_second_pos_range / decimated_sampleperiod);

                // print out the decoded bit, time, and bit number
                // this is very useful for debugging since we can quickly look up the relevant bit in the arrayview
                console_output.AppendFormat("{0,2}:{1,6} ", secondcount, max_time);
                currentdemodulator.DataSlicerResults.SecondSampleTimes[secondcount] = max_time;

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
                        sampler_threshold_autobias = second_sampling_variance/2;
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


            // do some statistics on the data
            double second_sampling_ratio_high_average = 0;
            int second_sampling_high_count = 0;
            double second_sampling_ratio_low_average = 0;
            int second_sampling_low_count = 0;
            double second_sampling_ratio_average = 0;
            double second_sampling_ratio_high_rms = 0;
            double second_sampling_ratio_low_rms = 0;

            foreach (double d in currentdemodulator.DataSlicerResults.SecondSampleRatios)
            {
                second_sampling_ratio_average += d;
                if (d > datasampler_threshold)
                {
                    second_sampling_ratio_high_rms += Math.Pow(d, 2);
                    second_sampling_ratio_high_average += d;
                    second_sampling_high_count++;
                }
                else
                {
                    second_sampling_ratio_low_rms += Math.Pow(d, 2);
                    second_sampling_ratio_low_average += d;
                    second_sampling_low_count++;
                }
            }

            second_sampling_ratio_high_rms = Math.Sqrt(second_sampling_ratio_high_rms / second_sampling_high_count);
            second_sampling_ratio_low_rms = Math.Sqrt(second_sampling_ratio_low_rms / second_sampling_low_count);

            second_sampling_ratio_average /= currentdemodulator.DataSlicerResults.SecondSampleRatios.Length;
            second_sampling_ratio_high_average /= second_sampling_high_count;
            second_sampling_ratio_low_average /= second_sampling_low_count;

            second_sampling_offset = ((second_sampling_ratio_high_average - second_sampling_ratio_low_average) / 2) + second_sampling_ratio_low_average;

            // midpoint should be 1 ideally

            console_output.AppendFormat("Data slicer ratio is {1:F4}, average value is {0:F4}. Offset: {2,3}, Scale: {3:F2}\r\n",
                second_sampling_ratio_average,
                second_sampling_offset, datasampler_ratio_offset, datasampler_bias_scale_offset);
            console_output.AppendFormat("     high average {0:F4} ({1,2}), low average {2:F4} ({3,2})\r\n", second_sampling_ratio_high_average, second_sampling_high_count
                , second_sampling_ratio_low_average, second_sampling_low_count);
            console_output.AppendFormat("High NR {0:F4} [dB], Low NR {1:F4} [dB], Sum {2:F4} [dB]\r\n", 10 * Math.Log10(second_sampling_ratio_high_rms), 10 * Math.Log10(second_sampling_ratio_low_rms),
                10 * Math.Log10(Math.Pow(second_sampling_ratio_high_rms, 2) + Math.Pow(second_sampling_ratio_low_rms, 2)));
        }

        private static double Calculate_Signal_SNR(double[] fm_filtered, double fm_filtered_square, int minutestart_sample, ref StringBuilder console_output)
        {
            double FM_Noise_rms = 0;
            double FM_Noise_sum = 0;
            // we can now attempt another SNR calculation, by using the known property of the signal: there is no modulation during the last second of a minute
            // iterate over the filtered data, centered on the minute correlation template
            for (int i = minutestart_sample - minute_correlator_template.Length / 2; i < minutestart_sample + minute_correlator_template.Length / 2; i++)
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



            console_output.AppendFormat("Modulation based SNR = {0}, or {1} dB\r\n", FM_Rectified_SNR, FM_Rectified_SNR_Log);
            return FM_Rectified_SNR;
        }

        private static void Find_Minute_Start(ref DemodulatorContext demodulator, TestSignalInfo _signal, ref StringBuilder console_output)
        {
            /* Find maximum value and assume this is the start of a minute 
             * Perform a LMS correlation looking for a bunch of zeros
             * This new version uses a convolution filter and some clever weighting of the output waveform to determine the minute start fairly well!
             */
            //bool minutestarted = false;
            double max_minute_correlation = double.NegativeInfinity;
            int minutestart_sample = 0;

            double decimated_sampleperiod = demodulator.DecimatedSamplePeriod;

            double[] minute_correlation_source = demodulator.MinuteDetectorParameters.MinuteDetectorSource;
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

            demodulator.MinuteDetectorParameters.MinuteDetectorCorrelationOutput = minute_convolved;
            demodulator.MinuteDetectorParameters.MinuteDetectorWeightedOutput = minute_convolved_weighted;

            // note that the recordings often actually start a second or two after the timestamp
            // due to how SDR-Console works
            console_output.AppendFormat("Found start of minute at time {0} ({1}), expected {2} ({3})\r\n", decimated_sampleperiod * minutestart_sample, minutestart_sample,
                _signal.ExpectedMinuteStartSeconds, _signal.ExpectedMinuteStartSeconds/decimated_sampleperiod);
            demodulator.MinuteDetectorParameters.MinuteDetectorResult = minutestart_sample;
        }

        private static void Perform_Correlations(ref DemodulatorContext currentdemodulator, ref StringBuilder console_output)
        {
            /* The technique for correlation here is to template match using least square error matching
                         * i.e. we are sensitive to the exact amplitude, not just the shape
                         * Also supports convolution, but this appears to offer no benefit vs. LMS correlation here
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

            // corrections to align correlation outputs with input data
            /*switch (_type)
            {
                case CorrelatorType.FM_Convolve:
                case CorrelatorType.FM_Convolve_Biased:
                    kerneldelay_zero = kerneldelay;
                    kerneldelay_one = kerneldelay + 9;
                    break;
                case CorrelatorType.PM_Convolve:
                case CorrelatorType.PM_Convolve_Biased:
                    kerneldelay_zero = kerneldelay - 6;
                    kerneldelay_one = kerneldelay - 6;
                    break;
                case CorrelatorType.FM:
                case CorrelatorType.FM_Biased:
                    kerneldelay_zero = -28;
                    kerneldelay_one = -24;
                    break;
            }*/

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
                    if (reverse_correlators)
                        zero_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_zero_correlator[(_zero_correlator.Length - 1) - j] - data_correlation_source[i + j], 2);
                    else
                        zero_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_zero_correlator[j] - data_correlation_source[i + j], 2);
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
                    if (reverse_correlators)
                        one_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_one_correlator[(_one_correlator.Length - 1) - j] - data_correlation_source[i + j], 2);
                    else
                        one_correlation[k < 0 ? 0 : k] += correlation_scale * Math.Pow(_one_correlator[j] - data_correlation_source[i + j], 2);
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

            if (currentdemodulator.IsFMBaseType())
            {
                // offset correct the correlators
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] -= zero_correlation_sum;
                    one_correlation[i] -= one_correlation_sum;
                    //one_correlation_min = Math.Min(one_correlation[i], one_correlation_min);
                    //zero_correlation_min = Math.Min(zero_correlation[i], zero_correlation_min);
                }
            }
            if (currentdemodulator.CorrelatorType == DemodulatorContext.CorrelatorTypeEnum.PM)
            {
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] -= zero_correlation_min;
                    one_correlation[i] -= one_correlation_min;
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

        private static void Demodulate(double[] i_filtered, double[] q_filtered, double[] fm_unfiltered, double[] pm_unfiltered, double[] fm_filtered, int movingaverage, out double fm_unfiltered_square, out double fm_filtered_square)
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

    }
}
