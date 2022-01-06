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

            Modes mode;
            mode = Modes.Standard;
            mode = Modes.Verify;
            int testindex = 5;

            DemodulatorContext currentdemodulator = GenerateDemodulator(DemodulatorDefaults.FM_Biased);

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

        public enum Modes
        {
            Standard,
            Verify
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

            using (FileStream wavefilestream = new FileStream(testsignal_current.FilePath_Base + testsignal_current.FilePath, FileMode.Open))
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
                using (FileStream wavefilestream = new FileStream(testsignal_current.FilePath_Base + testsignal_current.FilePath, FileMode.OpenOrCreate))
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
            Demodulate_To_FM(i_filtered, q_filtered, fm_unfiltered, pm_unfiltered, fm_filtered, demodulator.FilterParameters.FMAverageCount, out fm_unfiltered_square, out fm_filtered_square);
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

            demodulator.MinuteDetectorParameters.Source = fm_filtered_rectified;

            Correlate(ref demodulator, ref console_output);

            if (demodulator.MinuteDetectorParameters.Type == DemodulatorContext.MinuteDetectorTypeEnum.Convolver_Correlation)
                Find_Minute_Start_Convolver(ref demodulator, testsignal_current, ref console_output);
            else if (demodulator.MinuteDetectorParameters.Type == DemodulatorContext.MinuteDetectorTypeEnum.Correlator)
                Find_Minute_Start_Correlator(ref demodulator, testsignal_current, ref console_output);
            // TODO: Make some other types that are less resource heavy

            demodulator.DemodulationResult.FM_Rectified_SNR =  Calculate_Signal_SNR(fm_filtered, fm_filtered_square, demodulator.MinuteDetectorParameters.Result, ref console_output);

            Datasampler(ref demodulator, testsignal_current, ref console_output);


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


        private static void Print_Demodulated_Bits(bool[] payload_data, ref StringBuilder console_output)
        {
            foreach (bool bit in payload_data)
            {
                console_output.AppendFormat("{0}", bit ? 1 : 0);
            }
            console_output.AppendLine();
        }

    }
}
