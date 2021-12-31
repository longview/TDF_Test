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
            int testindex = 9;

            List<TestSignalInfo> testsignals = new List<TestSignalInfo>();
            // input file must be mono 16-bit, 20000 Hz (oddball rate)

            //0 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\websdr_recording_start_2021-12-28T12_57_51Z_157.0kHz.wav", 5000,
                "webSDR recording, high quality", 70, new DateTime(2021, 12, 28, 12, 58, 00, DateTimeKind.Utc)));
            //1 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T163350Z, 157 kHz, Wide-U.wav", 5000,
                "Ok signal, mid day", 30, new DateTime(2021, 12, 29, 16, 34, 00, DateTimeKind.Utc)));
            //2 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-29T185106Z, 157 kHz, Wide-U.wav", 5000,
                "Good signal, evening", 40, new DateTime(2021, 12, 29, 18, 52, 00, DateTimeKind.Utc)));
            //3 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T090027Z, 157 kHz, Wide-U.wav", 5000,
                "Medium signal, morning", 24, new DateTime(2021, 12, 30, 09, 01, 00, DateTimeKind.Utc)));
            //4 full of errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T102229Z, 157 kHz, Wide-U.wav", 5000,
                "Maintenance phase, off air", 0, new DateTime(2021, 12, 30, 10, 23, 00, DateTimeKind.Utc), 10, TestSignalInfo.Station_Status.Maintenance));
            //5 no errors, bit 19/20 is tricky
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T105034Z, 157 kHz, Wide-U.wav", 5000,
                "Medium signal, morning", 24, new DateTime(2021, 12, 30, 10, 51, 00, DateTimeKind.Utc)));
            //6 decodes with 5 errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T121742Z, 157 kHz, Wide-U_20.wav", 5000,
                "Poor signal, afternoon", 20, new DateTime(2021, 12, 30, 12, 18, 00, DateTimeKind.Utc), 5));
            //7 decodes with 3 errors, bit 48 is wrong
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T121914Z, 157 kHz, Wide-U_20.wav", 5000,
                "Poor signal, afternoon", 20, new DateTime(2021, 12, 30, 12, 20, 00, DateTimeKind.Utc), 3));
            //8 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T142316Z, 157 kHz, Wide-U.wav", 5000,
                "Poor signal, afternoon", 20, new DateTime(2021, 12, 30, 14, 24, 00, DateTimeKind.Utc)));
            //9 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T172433Z, 157 kHz, Wide-U.wav", 5000,
                "Good signal, early evening", 29, new DateTime(2021, 12, 30, 17, 25, 00, DateTimeKind.Utc)));
            //10 no errors
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T181314Z, 157 kHz, Wide-U.wav", 5000,
                "Good signal, early evening", 30, new DateTime(2021, 12, 30, 18, 14, 00, DateTimeKind.Utc)));
            // 11
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T200920Z, 157 kHz, Wide-U.wav", 5000,
                "Excellent signal, evening", 43, new DateTime(2021, 12, 30, 20, 10, 00, DateTimeKind.Utc)));
            // 12
            testsignals.Add(new TestSignalInfo("..\\..\\2021-12-30T235552Z, 157 kHz, Wide-U.wav", 5000,
                "Excellent signal, night, F1 set", 48, new DateTime(2021, 12, 30, 23, 56, 00, DateTimeKind.Utc)));

            TestSignalInfo testsignal_current = testsignals[testindex];

            if (mode == Modes.Standard)
            {
                Console.WriteLine("Using test index {0}, signal type {7}.\r\nFile {1} (IF = {6})\r\nSNR {2}, station was {3}.\r\nTime transmitted: {4}.\r\nComment: {5}",
                testindex, testsignal_current.FilePath, testsignal_current.SNR,
                testsignal_current.Status == TestSignalInfo.Station_Status.OnAir ? "on air" : "off air",
                testsignal_current.Recorded_Timestamp_UTC.ToString("o"), testsignal_current.Comment, testsignal_current.Frequency,
                testsignal_current.SignalType == TestSignalInfo.Signal_Type.TDF ? "TDF" : "DCF77 Phase");
            }

            CorrelatorType correlator_in_use = CorrelatorType.PM_Convolve;

            Console.WriteLine("Using {0} correlation", correlator_in_use.GetString());

            // generate correlators if desired
            if(false)
            {
#pragma warning disable CS0162 // Unreachable code detected
                StringBuilder console_output = new StringBuilder();
#pragma warning restore CS0162 // Unreachable code detected
                int zero = 1725;
                int one = 2926;
                Demodulate_Testsignal(testsignals[0], CorrelatorType.PM, ref console_output, true, zero, one);
                Demodulate_Testsignal(testsignals[0], CorrelatorType.FM, ref console_output, true, zero, one);
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
                    console_output.AppendFormat("Using test index {0}, signal type {7}.\r\nFile {1} (IF = {6})\r\nSNR {2}, station was {3}.\r\nTime transmitted: {4}.\r\nComment: {5}\r\n\r\n",
                    testsignals.IndexOf(signal), signal.FilePath, signal.SNR,
                    signal.Status == TestSignalInfo.Station_Status.OnAir ? "on air" : "off air",
                    signal.Recorded_Timestamp_UTC.ToString("o"), signal.Comment, signal.Frequency,
                    signal.SignalType == TestSignalInfo.Signal_Type.TDF ? "TDF" : "DCF77 Phase");

                    // parameter sweep function
                    // used to determine optimal coefficients by linear search
                    /*for (int i = 0; i < 20; i++)
                    {
                        errors = 0;
                        datasampler_ratio_offset = -0.5 * (double)i/5 + 1;
                        errors = Demodulate_Testsignal(signal, ref console_output, datasampler_bias_scale_offset, datasampler_ratio_offset);
                        sweep_results[i] += errors;
                        if (errors != signal.Expected_Errors)
                            fail_count++;

                        Console.WriteLine("Index {0:D2}, expected errors {1}, found {2}, {3} [{4}]", testsignals.IndexOf(signal), signal.Expected_Errors, errors, datasampler_bias_scale_offset, i);
                    }*/

                    errors = Demodulate_Testsignal(signal, correlator_in_use, ref console_output);
                    if (errors > signal.Expected_Errors)
                        fail_count++;
                    else if (errors < signal.Expected_Errors)
                        antifail_count++;

                    Console.WriteLine("Index {0:D2}, expected errors {1}, found {2}{4}. Comment: {3}",
                        testsignals.IndexOf(signal), signal.Expected_Errors, errors, signal.Comment, errors < signal.Expected_Errors ? " (better than expected!)" : "");

                    File.WriteAllText(String.Format("Verify_Result_{0}_f{1}_e{2}_{3}.txt", testsignals.IndexOf(signal), errors, signal.Expected_Errors, correlator_in_use.GetString()),
                        console_output.ToString());
                }

                Console.WriteLine("Finished tests, {0} failures of {1} total{2}", fail_count, testsignals.Count, antifail_count > 0 ? String.Format(". {0} test(s) exceeded specification!", antifail_count) : ".");
            }
            else
            {
                StringBuilder console_output = new StringBuilder();

                Demodulate_Testsignal(testsignal_current, correlator_in_use, ref console_output);

                Console.Write(console_output.ToString());
            }

            Console.ReadLine();

        }

        public struct TestSignalInfo
        {
            public TestSignalInfo(string _filepath, double _frequency, string _comment, double _snr, DateTime _date, int _errors = 0, Station_Status _status = Station_Status.OnAir, Signal_Type _signaltype = Signal_Type.TDF)
            {
                FilePath = _filepath;
                Comment = _comment;
                SNR = _snr;
                Frequency = _frequency;
                Status = _status;
                // add 1 minute to timestamp from start of recording timestamp
                Recorded_Timestamp_UTC = _date.AddMinutes(1);
                SignalType = _signaltype;
                Expected_Errors = _errors;
            }
            public string FilePath;
            public string Comment;
            public double Frequency;
            public double SNR;
            public Station_Status Status;
            public DateTime Recorded_Timestamp_UTC;
            public Signal_Type SignalType;
            public int Expected_Errors;
            public enum Station_Status
            {
                OnAir,
                Maintenance
            }

            public enum Signal_Type
            {
                TDF, DCFp
            }
        }

        public enum Modes
        {
            Standard,
            Verify
        }



        private static int Demodulate_Testsignal(TestSignalInfo testsignal_current, CorrelatorType _correlatortype,
            ref StringBuilder console_output, 
            bool generate_correlator = false, int zero_offset = 0, int one_offset = 0)
        {
            // frequency offset of USB receiver
            double frequency = testsignal_current.Frequency;

            double samplerate = 20000;
            double sampleperiod = 1 / samplerate;

            double[] data;
            double[] data_right;
            openWav(testsignal_current.FilePath, out data, out data_right);
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

            NWaves.Filters.MovingAverageFilter fm_lpf = new NWaves.Filters.MovingAverageFilter(8);
            NWaves.Filters.MovingAverageFilter fm_rectified_lpf = new NWaves.Filters.MovingAverageFilter(64);
            console_output.AppendFormat("FM moving average filter size {0}\r\nFM rectifier filter size {1}\r\n", fm_lpf.Size, fm_rectified_lpf.Size);
            double fm_unfiltered_square, fm_filtered_square;
            Demodulate(i_filtered, q_filtered, fm_unfiltered, pm_unfiltered, fm_filtered, fm_lpf, out fm_unfiltered_square, out fm_filtered_square);
            Perform_PM_Correction(phase_error_per_sample_vs_frequency, ref pm_unfiltered, pm_filtered_drift, ref console_output);

            if (generate_correlator && (_correlatortype == CorrelatorType.FM || _correlatortype == CorrelatorType.FM_Convolve))
                Generate_Correlators(fm_filtered, _correlatortype, zero_offset, one_offset);
            else if (generate_correlator && _correlatortype == CorrelatorType.PM)
                Generate_Correlators(pm_filtered_drift, _correlatortype, zero_offset, one_offset);
            // attempt an SNR calculation based on full band (signal and noise)
            // and filtered square values (mostly just signal we assume)

            FM_SNR_Calculation(fm_unfiltered, fm_filtered, ref fm_unfiltered_square, ref fm_filtered_square, ref console_output);
            
            double[] fm_filtered_rectified = Generate_Rectified_FM(data, IQ_decimation_factor, fm_filtered, fm_lpf, fm_rectified_lpf);

            double[] minute_start_correlation, zero_correlation, one_correlation;
            if (_correlatortype == CorrelatorType.FM || _correlatortype == CorrelatorType.FM_Convolve)
                Perform_Correlations(ref fm_filtered, ref fm_filtered_rectified, ref zero_correlator_template_FM, 
                    ref one_correlator_template_FM, out minute_start_correlation, out zero_correlation, out one_correlation, _correlatortype, ref console_output);
            else
                Perform_Correlations(ref pm_filtered_drift, ref fm_filtered_rectified, ref zero_correlator_template_PM, 
                    ref one_correlator_template_PM, out minute_start_correlation, out zero_correlation, out one_correlation, _correlatortype, ref console_output);
            // TODO: also correlate on PM for minute start, or use a rectified PM output?


            int minutestart_sample = Find_Minute_Start(decimated_sampleperiod, fm_unfiltered, minute_start_correlation, ref console_output);

            Calculate_Signal_SNR(fm_filtered, fm_filtered_square, minutestart_sample, ref console_output);
            int datasampler_stop;
            bool[] payload_data;
            double[] second_sampling_ratio;
            double[] second_sampling_times;


            Perform_Detection(decimated_sampleperiod, fm_unfiltered, zero_correlation, one_correlation,
                minutestart_sample, out datasampler_stop, out payload_data, out second_sampling_ratio,
                out second_sampling_times, _correlatortype, ref console_output);
            Print_Demodulated_Bits(decimated_sampleperiod, datasampler_stop, payload_data, ref console_output);
            Print_Demodulated_Bits_Informative(console_output, zero_correlation, one_correlation, payload_data, second_sampling_ratio);

            int decode_error_count = Decode_Received_Data(testsignal_current, payload_data, ref console_output);

            return decode_error_count;
        }

        private static void Print_Demodulated_Bits_Informative(StringBuilder console_output, double[] zero_correlation, double[] one_correlation, bool[] payload_data, double[] second_sampling_ratio)
        {
            // print out informative data to aid debugging:
            int count = 0;
            console_output.AppendFormat("No.  Sym  Value   Expct   Rat\r\n");
            console_output.AppendFormat("00   M    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("01   A2   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("02   A3   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("03   HA2  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("04   HA4  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("05   HA8  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("06   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("07   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("08   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("09   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("10   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("11   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("12   0    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("13   F1   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("14   F2   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("15   0    {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("16   A1   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("17   Z1   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("18   Z2   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("19   X    {0,5}   False   {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("20   S    {0,5}   True    {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("21   M01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("22   M02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("23   M04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("24   M08  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("25   M10  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("26   M20  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("27   M40  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("28   P1   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("29   H01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("30   H02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("31   H04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("32   H08  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("33   H10  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("34   H20  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("35   P2   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("36  DM01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("37  DM02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("38  DM04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("39  DM08  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("40  DM10  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("41  DM20  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("42  DW01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("43  DW02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("44  DW04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("45  MO01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("46  MO02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("47  MO04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("48  MO08  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("49  MO10  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("50   Y01  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("51   Y02  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("52   Y04  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("53   Y08  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("54   Y10  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("55   Y20  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("56   Y40  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("57   Y80  {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
            count++;
            console_output.AppendFormat("58   P3   {0,5}   N/A     {1:F4}\r\n",
                payload_data[count].ToString(), second_sampling_ratio[count]);
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
                console_output.AppendFormat("Month {0} is outside of allowable range (1-13)\r\n", month);
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

                if ((int)decoded_time.DayOfWeek != day_of_week)
                {
                    console_output.AppendFormat("Decoded day of week is wrong, {0} should be {1}\r\n", day_of_week, decoded_time.DayOfWeek);
                    decode_error_count++;
                }
                else
                {
                    console_output.AppendFormat("Decoded day of week seems correct ({0})\r\n", decoded_time.DayOfWeek.ToString());
                }

                // check if the time is equal to the test recording info
                if (testsignal_current.Recorded_Timestamp_UTC.CompareTo(decoded_time.ToUniversalTime()) == 0)
                    console_output.AppendFormat("Decoded time matches recording timestamp.\r\n");
                else
                {
                    console_output.AppendFormat("Decoded time does not match timestamp ({0}).\r\n", testsignal_current.Recorded_Timestamp_UTC.ToUniversalTime().ToString("o"));
                    TimeSpan decoded_time_error = testsignal_current.Recorded_Timestamp_UTC - decoded_time.ToUniversalTime();
                    console_output.AppendFormat("Decoded time error is: {0} (D:HH:MM:SS,SS')\r\n", decoded_time_error.ToString("G"));
                    decode_error_count++;
                }
            }
            catch (Exception)
            {
                console_output.AppendFormat("Decoded date and time is not valid.\r\n");
                decode_error_count++;
            }
            console_output.AppendFormat("Decode found {0} errors, expected {2}, SNR {1})\r\n", decode_error_count, testsignal_current.SNR, testsignal_current.Expected_Errors);
            if (decode_error_count < testsignal_current.Expected_Errors)
                console_output.AppendFormat("Error count ({0}) was better than specified!\r\n", decode_error_count);
            if (testsignal_current.Status == TestSignalInfo.Station_Status.Maintenance)
                console_output.AppendFormat("Station was known to be off air, errors are expected.\r\n");
            console_output.AppendFormat("Finished\r\n");
            return decode_error_count;
        }

        private static void Print_Demodulated_Bits(double decimated_sampleperiod, int datasampler_stop, bool[] payload_data, ref StringBuilder console_output)
        {
            console_output.AppendFormat("Decoded {0} bits: \r\n", payload_data.Length, datasampler_stop * decimated_sampleperiod);
            foreach (bool bit in payload_data)
            {
                console_output.AppendFormat("{0}", bit ? 1 : 0);
            }
            console_output.AppendLine();
        }

        private static void Perform_Detection(double decimated_sampleperiod,
            double[] fm_unfiltered,
            double[] zero_correlation,
            double[] one_correlation,
            int minutestart_sample,
            out int datasampler_stop,
            out bool[] payload_data,
            out double[] second_sampling_ratio,
            out double[] second_sampling_times, CorrelatorType _correlatortype, ref StringBuilder console_output
            )
        {

            double datasampler_bias_scale_offset = 0;
            // offset to the ratio of one/zero
            double datasampler_ratio_offset = 0;

            if (_correlatortype == CorrelatorType.FM)
            {
                // 0 is the optimal value
                datasampler_bias_scale_offset = 0;
                // this is the optimal value
                datasampler_ratio_offset = -0.1;
            }
            else if (_correlatortype == CorrelatorType.FM_Convolve)
            {
                // 0 is the optimal value
                datasampler_bias_scale_offset = 0;
                // this is the optimal value
                datasampler_ratio_offset = -0.3;
            }
            else if (_correlatortype == CorrelatorType.PM_Convolve)
            {
                // 0 is the optimal value
                datasampler_bias_scale_offset = 0;
                // this is the optimal value
                datasampler_ratio_offset = -0.3;
            }
            else if (_correlatortype == CorrelatorType.PM)
            {
                // 0 is the optimal value
                datasampler_bias_scale_offset = 0;
                // this is the optimal value
                datasampler_ratio_offset = 0.3;
            }


            int datasampler_start = minutestart_sample + (int)(0.75 / decimated_sampleperiod);
            datasampler_stop = minutestart_sample + (int)(1.0 / decimated_sampleperiod);
            payload_data = new bool[59];
            second_sampling_times = new double[59];
            second_sampling_ratio = new double[59];

            double datasampler_bias_scale = 0;


            double second_sampling_offset = 0;

            // this should be relatively wide unless the minute-start detector is very accurate
            // the SNR for the first second is usually good, so a wider window doesn't seem to hurt performance
            datasampler_start = minutestart_sample + (int)(0.75 / decimated_sampleperiod);
            datasampler_stop = minutestart_sample + (int)(1.0 / decimated_sampleperiod);
            int secondcount = 0;

            console_output.AppendFormat("Next bit expected at: ({1}){0}", (datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod, 0);

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
                // for a real-time system we can expect to know what most of the bits are already
                // so we could (very carefully) bias the detector to the expected value
                // obviously this must be done carefully since otherwise we will never update our expectations if something changes
                // we could perhaps do both (i.e. record both an actual reading + the expected), time bits are expected to change linearly, and status bits are expected to
                // be static every hour (and in most cases static for many hours). this can be used to average out errors in most cases.
                if (secondcount == 0)
                {
                    // correct based on first measurement
                    datasampler_bias_scale = max_zero / max_one;
                    // correct manually to make it work better in poor SNR
                    datasampler_bias_scale *= 1 + datasampler_bias_scale_offset;
                    // correct for template length; slight layering violation
                    // this check only applies to standard correlation, not the convolvers
                    if (_correlatortype == CorrelatorType.FM)
                        datasampler_bias_scale *= (double)zero_correlator_template_FM.Length / (double)one_correlator_template_FM.Length;
                    else if (_correlatortype == CorrelatorType.PM)
                        datasampler_bias_scale *= (double)zero_correlator_template_PM.Length / (double)one_correlator_template_PM.Length;
                }

                max_one *= datasampler_bias_scale;


                double ratio = 0;
                // try to handle some edge cases
                if (max_zero <= 0 || max_one <= 0)
                {
                    if (max_zero <= 0)
                        ratio = 1000;
                    if (max_one <= 0)
                        ratio = 0;
                }
                else
                {
                    ratio = max_one / max_zero;
                }
                    

                ratio += datasampler_ratio_offset;

                // store the ratio for debug analysis
                second_sampling_ratio[secondcount] = ratio;

                // at this point we could in future try to do e.g. a 2nd order polynomial curve fit
                // to improve our time resolution

                bool bit = ratio > 1;

                payload_data[secondcount] = bit;

                // we now look for the bits within a small time window to improve detection probability
                // we are effectively phase locked now and should get the correct sample point very precisely for all future seconds
                if (bit)
                {
                    datasampler_start = max_one_time + (int)(0.97 / decimated_sampleperiod);
                    datasampler_stop = max_one_time + (int)(1.03 / decimated_sampleperiod);
                }
                else
                {
                    datasampler_start = max_zero_time + (int)(0.97 / decimated_sampleperiod);
                    datasampler_stop = max_zero_time + (int)(1.03 / decimated_sampleperiod);
                }

                // print out the decoded bit, time, and bit number
                // this is very useful for debugging since we can quickly look up the relevant bit in the arrayview
                console_output.AppendFormat(":{2} ({1}){0}", (datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod, secondcount + 1, bit ? "1" : "0");
                // add all but the first second sample point (first one is usually slightly off)
                second_sampling_times[secondcount] = (datasampler_start + (datasampler_stop - datasampler_start)) * decimated_sampleperiod;

                secondcount++;
            }

            console_output.AppendLine();


            // do some statistics on the data
            double second_sampling_ratio_high_average = 0;
            int second_sampling_high_count = 0;
            double second_sampling_ratio_low_average = 0;
            int second_sampling_low_count = 0;
            double second_sampling_ratio_average = 0;
            double second_sampling_ratio_high_rms = 0;
            double second_sampling_ratio_low_rms = 0;

            foreach (double d in second_sampling_ratio)
            {
                second_sampling_ratio_average += d;
                if (d > 1)
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

            second_sampling_ratio_high_rms = Math.Sqrt(second_sampling_ratio_high_rms/second_sampling_high_count);
            second_sampling_ratio_low_rms = Math.Sqrt(second_sampling_ratio_low_rms/second_sampling_low_count);

            second_sampling_ratio_average /= second_sampling_ratio.Length;
            second_sampling_ratio_high_average /= second_sampling_high_count;
            second_sampling_ratio_low_average /= second_sampling_low_count;

            second_sampling_offset = ((second_sampling_ratio_high_average - second_sampling_ratio_low_average) / 2) + second_sampling_ratio_low_average;

            // midpoint should be 1 ideally
            
            console_output.AppendFormat("Data slicer ratio is {1}, average value is {0}. Offset: {2}, Scale: {3}\r\n",
                second_sampling_ratio_average,
                second_sampling_offset, datasampler_ratio_offset, datasampler_bias_scale_offset);
            console_output.AppendFormat("     high average {0} ({1}), low average {2} ({3})\r\n", second_sampling_ratio_high_average, second_sampling_high_count
                , second_sampling_ratio_low_average, second_sampling_low_count);
            console_output.AppendFormat("High NR {0} [dB], Low NR {1} [dB], Sum {2} [dB]\r\n", 10 * Math.Log10(second_sampling_ratio_high_rms), 10 * Math.Log10(second_sampling_ratio_low_rms),
                10 * Math.Log10(Math.Pow(second_sampling_ratio_high_rms, 2) + Math.Pow(second_sampling_ratio_low_rms,2)));


            double second_delta_rms = 0;
            for (int i = 1; i < second_sampling_times.Length; i++)
            {
                second_delta_rms += second_sampling_times[i] - second_sampling_times[i - 1];
            }
            second_delta_rms /= second_sampling_times.Length - 1;
            //second_delta_rms = Math.Abs(1-second_delta_rms);


            console_output.AppendFormat("Second delta average: {0} ms\r\n", second_delta_rms * 1000);
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

        private static int Find_Minute_Start(double decimated_sampleperiod, double[] fm_unfiltered, double[] minute_start_correlation, ref StringBuilder console_output)
        {
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

            console_output.AppendFormat("Found start of minute at time {0}\r\n", decimated_sampleperiod * minutestart_sample);
            return minutestart_sample;
        }

        private static void Perform_Correlations(ref double[] data_correlation_source,
            ref double[] minute_correlation_source, ref double[] _zero_correlator, ref double[] _one_correlator, out double[] minute_start_correlation,
            out double[] zero_correlation, out double[] one_correlation, CorrelatorType _type, ref StringBuilder console_output)
        {
            /* The technique for correlation here is to template match using least square error matching
                         * i.e. we are sensitive to the exact amplitude, not just the shape
                         * We first find the start of a minute using template 3 on the *rectified* FM data
                         * this then determines where we look in the bit 0 or 1 sets
                         */


            console_output.AppendFormat("Doing correlations in {0} mode.\r\n", _type.GetString());
            minute_start_correlation = new double[data_correlation_source.Length];
            double minute_start_correlation_sum = 0;

            NWaves.Operations.Convolution.OlaBlockConvolver con_zero = null;
            NWaves.Operations.Convolution.OlaBlockConvolver con_one = null;
            NWaves.Operations.Convolution.OlaBlockConvolver con_minute = null;
            int kernelsize = 128;
            if (_type == CorrelatorType.FM_Convolve || _type == CorrelatorType.PM_Convolve)
            {
                
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

                /*float[] convolver_kernel_minute = new float[minute_correlator_template.Length];
                for (int i = 0; i < _one_correlator.Length; i++)
                {
                    convolver_kernel_one[i] = 0;
                }

                con_minute = new NWaves.Operations.Convolution.OlaBlockConvolver(convolver_kernel_minute, kernelsize);*/

            }



            double correlation_scale = -1;

            // correlation for minute start, just zeros of a given length
            // this should perhaps not be a correlator for efficiency?
            for (int i = 0; i < minute_correlation_source.Length - minute_correlator_template.Length; i++)
            {
                /*if (_type == CorrelatorType.FM_Convolve && con_minute != null)
                {
                    minute_start_correlation[i] = con_minute.Process((float)data_correlation_source[i]);
                    continue;
                }*/

                for (int j = 0; j < minute_correlator_template.Length; j++)
                {
                    minute_start_correlation[i] += -1*Math.Pow(minute_correlator_template[j] - minute_correlation_source[i + j], 2);
                }

                minute_start_correlation_sum += minute_start_correlation[i];

            }

            minute_start_correlation_sum /= minute_start_correlation.Length;

            zero_correlation = new double[data_correlation_source.Length];
            double zero_correlation_sum = 0;

            // correlation 1, look for second start with data 0
            for (int i = 0; i < data_correlation_source.Length - _zero_correlator.Length; i++)
            {
                if ((_type == CorrelatorType.FM_Convolve || _type == CorrelatorType.PM_Convolve) && con_zero != null)
                {
                    // dump the kernel size to avoid delay
                    int j = i-kernelsize; 

                    zero_correlation[j < 0 ? 0 : j] = con_zero.Process((float)data_correlation_source[i]);
                    continue;
                }

                for (int j = 0; j < _zero_correlator.Length; j++)
                {
                    if (_type == CorrelatorType.PM)
                        zero_correlation[i] += correlation_scale * Math.Pow(_zero_correlator[j] - data_correlation_source[i + j], 2);
                    //zero_correlation[i] = (1+_zero_correlator[j]) * data_correlation_source[i + j];
                    else
                        zero_correlation[i] += correlation_scale*Math.Pow(_zero_correlator[j] - data_correlation_source[i + j], 2);
                    //correlation1[i] += (correlator_1[j] > 0 ? 1 : 0) * fm_filtered[i + j];
                    //correlation1[i] += (correlator_1[j] > 0 ? 0:1) ^ (fm_filtered[i + j] > 0 ? 1 : 0);
                }
                zero_correlation_sum += zero_correlation[i];

            }
            zero_correlation_sum /= zero_correlation.Length;

            one_correlation = new double[data_correlation_source.Length];
            double one_correlation_sum = 0;

            // correlation 1, look for second start with data 0
            for (int i = 0; i < data_correlation_source.Length - _one_correlator.Length; i++)
            {
                if ((_type == CorrelatorType.FM_Convolve || _type == CorrelatorType.PM_Convolve) && con_one != null)
                {
                    int j = i - kernelsize;
                    one_correlation[j < 0 ? 0 : j] = con_one.Process((float)data_correlation_source[i]);
                    continue;
                }

                for (int j = 0; j < _one_correlator.Length; j++)
                {
                    if (_type == CorrelatorType.PM)
                        one_correlation[i] += correlation_scale * Math.Pow(_one_correlator[j] - data_correlation_source[i + j], 2);
                    //one_correlation[i] += (1+_one_correlator[j]) * data_correlation_source[i + j];
                    else
                        one_correlation[i] += correlation_scale*Math.Pow(_one_correlator[j] - data_correlation_source[i + j], 2);
                    //correlation1[i] += (correlator_1[j] > 0 ? 1 : 0) * fm_filtered[i + j];
                    //correlation1[i] += (correlator_1[j] > 0 ? 0:1) ^ (fm_filtered[i + j] > 0 ? 1 : 0);
                }

                one_correlation_sum += one_correlation[i];

            }


            one_correlation_sum /= one_correlation.Length;

            if (_type == CorrelatorType.FM)
            {
                // offset correct the correlators
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] -= zero_correlation_sum;
                    one_correlation[i] -= one_correlation_sum;
                    minute_start_correlation[i] -= minute_start_correlation_sum;
                }
            }
            else if (_type == CorrelatorType.FM_Convolve || _type == CorrelatorType.PM_Convolve)
            {
                //NWaves.Filters.MovingAverageFilter corr_zero_lpf = new MovingAverageFilter(4);
                //NWaves.Filters.MovingAverageFilter corr_one_lpf = new MovingAverageFilter(4);
                //NWaves.Filters.MovingAverageFilter corr_minute_lpf = new MovingAverageFilter(16);
                // square the data
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    //zero_correlation[i] = corr_zero_lpf.Process((float)zero_correlation[i]);//((float)Math.Pow(zero_correlation[i],2));
                    //one_correlation[i] = corr_one_lpf.Process((float)zero_correlation[i]);//((float)Math.Pow(one_correlation[i], 2));
                    //minute_start_correlation[i] = corr_minute_lpf.Process((float)Math.Pow(minute_start_correlation[i], 2));
                }
                /*
                // try to window the data, maybe filter it idk
                float[] window = NWaves.Windows.Window.Flattop(16);
                double[] window_d = new double[window.Length];
                for (int i = 0; i < window.Length; i++)
                    window_d[i] = (double)window[i];
                NWaves.Windows.WindowExtensions.ApplyWindow(zero_correlation, window_d);
                NWaves.Windows.WindowExtensions.ApplyWindow(one_correlation, window_d);
                //NWaves.Windows.WindowExtensions.ApplyWindow(minute_start_correlation, window_d);*/

            }
            else if (_type == CorrelatorType.PM)
            {
                NWaves.Filters.DcRemovalFilter dc_filter_one = new DcRemovalFilter(0.9);
                NWaves.Filters.DcRemovalFilter dc_filter_zero = new DcRemovalFilter(0.9);
                for (int i = 0; i < zero_correlation.Length; i++)
                {
                    zero_correlation[i] = dc_filter_zero.Process((float)zero_correlation[i]) + 1e4;
                    one_correlation[i] = dc_filter_one.Process((float)one_correlation[i]) + 1e4;
                }
            }
        }

        /* Generate correlators, input is FM data to sample, and the UTC 0 ms position of the two waveforms */
        private static void Generate_Correlators(double[] correlator_template_source, CorrelatorType _type, int zero_offset, int one_offset)
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
                correlation_output.AppendFormat("{0},", (correlator_template_source[i] - (average_value/56))/ template_scale);
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

        private static double[] Generate_Rectified_FM(double[] data, int IQ_decimation_factor, double[] fm_filtered, MovingAverageFilter fm_lpf, MovingAverageFilter fm_rectified_lpf)
        {
            // rectify fm_filtered, might be good?
            fm_lpf.Reset();
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

            //for (int i = 0; i < pm_filtered.Length; i++)
            //{
            //    pm_drift += pm_filtered_drift[i];
            //}
            //double pm_drift_rate = pm_drift / pm_filtered.Length;
            // average drift per sample
            double pm_drift_rate = pm_drift / (pm_unfiltered.Length - pm_unfiltered.Length / 4);

            NWaves.Filters.DcRemovalFilter dc_filter = new DcRemovalFilter(0.9);

            // stabilize filter
            for (int i = 0; i < pm_unfiltered.Length; i++)
            { 
                dc_filter.Process((float)pm_unfiltered[i]);
            }

            // correct pm filtered data
            for (int i = 0; i < pm_unfiltered.Length; i++)
            {
                //pm_filtered_drift[i] = pm_unfiltered[i] - (pm_drift_rate * i);
                pm_filtered_drift[i] = dc_filter.Process((float)pm_unfiltered[i]);
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

        private static void Demodulate(double[] i_filtered, double[] q_filtered, double[] fm_unfiltered, double[] pm_unfiltered, double[] fm_filtered, MovingAverageFilter fm_lpf, out double fm_unfiltered_square, out double fm_filtered_square)
        {
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
            NWaves.Filters.MovingAverageFilter i_lpf = new NWaves.Filters.MovingAverageFilter(averagecount);
            NWaves.Filters.MovingAverageFilter q_lpf = new NWaves.Filters.MovingAverageFilter(averagecount);
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


    public enum CorrelatorType
    {
        FM,
        PM,
        FM_Convolve,
        PM_Convolve
    }

    public static class CorrelatorTypeExtension
    {
        public static string GetString(this CorrelatorType me)
        {
            switch (me)
            {
                case CorrelatorType.FM:
                    return "FM";
                case CorrelatorType.PM:
                    return "PM";
                case CorrelatorType.FM_Convolve:
                    return "FM Convolver";
                case CorrelatorType.PM_Convolve:
                    return "PM Convolver";
                default:
                    return "Unknown";
            }
        }
    }
}
