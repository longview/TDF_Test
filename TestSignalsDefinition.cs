using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    partial class Program
    {
        private static void PopulateTestSignals(List<TestSignalInfo> testsignals)
        {

            // error rates are for FM_Biased
            // FM without bias should perform within a few errors of this
            // mean-variance demodulator should perform better for all signals

            //0 no errors
            testsignals.Add(new TestSignalInfo("websdr_recording_start_2021-12-28T12_57_51Z_157.0kHz.wav", "webSDR recording, high quality",
                70, new DateTime(2021, 12, 28, 12, 57, 51, DateTimeKind.Utc)));
            //1 no errors
            testsignals.Add(new TestSignalInfo("2021-12-29T163350Z, 157 kHz, Wide-U.wav", "Ok signal, mid day",
                30, new DateTime(2021, 12, 29, 16, 33, 50, DateTimeKind.Utc)));
            //2 no errors
            testsignals.Add(new TestSignalInfo("2021-12-29T185106Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                40, new DateTime(2021, 12, 29, 18, 51, 06, DateTimeKind.Utc)));
            //3 no errors
            testsignals.Add(new TestSignalInfo("2021-12-30T090027Z, 157 kHz, Wide-U.wav", "Medium signal, morning",
                24, new DateTime(2021, 12, 30, 09, 00, 27, DateTimeKind.Utc)));
            //4 full of errors
            testsignals.Add(new TestSignalInfo("2021-12-30T102229Z, 157 kHz, Wide-U.wav", "Maintenance phase, off air",
                0, new DateTime(2021, 12, 30, 10, 22, 29, DateTimeKind.Utc), _errors: 25, _status: TestSignalInfo.Station_Status.Maintenance));
            //5 no errors, bit 19/20 is tricky
            testsignals.Add(new TestSignalInfo("2021-12-30T105034Z, 157 kHz, Wide-U.wav", "Medium signal, morning",
                24, new DateTime(2021, 12, 30, 10, 50, 34, DateTimeKind.Utc)));
            //6 decodes with 5 errors
            testsignals.Add(new TestSignalInfo("2021-12-30T121742Z, 157 kHz, Wide-U_20.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 12, 17, 42, DateTimeKind.Utc), _errors: 4));
            //7 
            testsignals.Add(new TestSignalInfo("2021-12-30T121914Z, 157 kHz, Wide-U_20.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 12, 19, 14, DateTimeKind.Utc)));
            //8
            testsignals.Add(new TestSignalInfo("2021-12-30T142316Z, 157 kHz, Wide-U.wav", "Poor signal, afternoon",
                20, new DateTime(2021, 12, 30, 14, 23, 16, DateTimeKind.Utc)));
            //9
            testsignals.Add(new TestSignalInfo("2021-12-30T172433Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                29, new DateTime(2021, 12, 30, 17, 24, 33, DateTimeKind.Utc)));
            //10
            testsignals.Add(new TestSignalInfo("2021-12-30T181314Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                30, new DateTime(2021, 12, 30, 18, 13, 14, DateTimeKind.Utc)));
            // 11
            testsignals.Add(new TestSignalInfo("2021-12-30T200920Z, 157 kHz, Wide-U.wav", "Excellent signal, evening",
                43, new DateTime(2021, 12, 30, 20, 09, 20, DateTimeKind.Utc)));
            // 12
            testsignals.Add(new TestSignalInfo("2021-12-30T235552Z, 157 kHz, Wide-U.wav", "Excellent signal, night",
                48, new DateTime(2021, 12, 30, 23, 55, 52, DateTimeKind.Utc)));
            // 13 - tricky start, minute is around 7000
            testsignals.Add(new TestSignalInfo("2021-12-31T181322Z, 157 kHz, Wide-U.wav", "Poor signal, evening",
                22, new DateTime(2021, 12, 31, 18, 13, 22, DateTimeKind.Utc), _errors: 0));
            // 14 - minute start around 6500
            testsignals.Add(new TestSignalInfo("2021-12-31T181524Z, 157 kHz, Wide-U.wav", "Poor signal, evening",
                22, new DateTime(2021, 12, 31, 18, 15, 24, DateTimeKind.Utc), _errors: 0));
            // 15
            testsignals.Add(new TestSignalInfo("2021-12-31T222827Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                20, new DateTime(2021, 12, 31, 22, 28, 27, DateTimeKind.Utc)));
            // 16 - last of the year :)
            testsignals.Add(new TestSignalInfo("2021-12-31T225740Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 57, 40, DateTimeKind.Utc), _errors: 0));
            // 17 - first of the year
            testsignals.Add(new TestSignalInfo("2021-12-31T225835Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 58, 35, DateTimeKind.Utc), _errors: 0));
            // 18
            testsignals.Add(new TestSignalInfo("2021-12-31T225930Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2021, 12, 31, 22, 59, 30, DateTimeKind.Utc)));
            // 19
            testsignals.Add(new TestSignalInfo("2022-01-02T110116Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                15, new DateTime(2022, 01, 02, 11, 01, 20, DateTimeKind.Utc), _errors: 7));
            // 20
            testsignals.Add(new TestSignalInfo("2022-01-02T115821Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                18, new DateTime(2022, 01, 02, 11, 58, 22, DateTimeKind.Utc), _errors: 0));
            // 21
            testsignals.Add(new TestSignalInfo("2022-01-02T130333Z, 157 kHz, Wide-U.wav", "Poor signal, mid day",
                16, new DateTime(2022, 01, 02, 13, 03, 33, DateTimeKind.Utc), _errors: 16));
            // 22
            testsignals.Add(new TestSignalInfo("2022-01-02T155905Z, 157 kHz, Wide-U.wav", "Good signal, afternoon",
                34, new DateTime(2022, 01, 02, 15, 59, 05, DateTimeKind.Utc)));
            // 23
            testsignals.Add(new TestSignalInfo("2022-01-02T172940Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                45, new DateTime(2022, 01, 02, 17, 29, 40, DateTimeKind.Utc)));
            // 24
            testsignals.Add(new TestSignalInfo("2022-01-02T185525Z, 157 kHz, Wide-U.wav", "Near perfect, evening",
                55, new DateTime(2022, 01, 02, 18, 55, 25, DateTimeKind.Utc)));
            // 25
            testsignals.Add(new TestSignalInfo("2022-01-02T193314Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                45, new DateTime(2022, 01, 02, 19, 33, 14, DateTimeKind.Utc)));
            // 26
            testsignals.Add(new TestSignalInfo("2022-01-02T194820Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                44, new DateTime(2022, 01, 02, 19, 48, 20, DateTimeKind.Utc)));
            // 27
            testsignals.Add(new TestSignalInfo("2022-01-02T195751Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                43, new DateTime(2022, 01, 02, 19, 57, 51, DateTimeKind.Utc)));
            // 28
            testsignals.Add(new TestSignalInfo("2022-01-02T200634Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                42, new DateTime(2022, 01, 02, 20, 06, 34, DateTimeKind.Utc)));
            // 29
            testsignals.Add(new TestSignalInfo("2022-01-02T213646Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                46, new DateTime(2022, 01, 02, 21, 36, 46, DateTimeKind.Utc)));
            // 30
            testsignals.Add(new TestSignalInfo("2022-01-05T180209Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                36, new DateTime(2022, 01, 05, 18, 02, 09, DateTimeKind.Utc)));
            // 31
            testsignals.Add(new TestSignalInfo("2022-01-05T191642Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                30, new DateTime(2022, 01, 05, 19, 16, 42, DateTimeKind.Utc)));
            // 32
            testsignals.Add(new TestSignalInfo("websdr_recording_start_2022-01-05T19_23_08Z_157.0kHz.wav", "webSDR with lightning",
                60, new DateTime(2022, 01, 05, 19, 23, 08, DateTimeKind.Utc)));
            // 33
            testsignals.Add(new TestSignalInfo("websdr_recording_start_2022-01-05T19_25_44Z_157.0kHz.wav", "webSDR",
                60, new DateTime(2022, 01, 05, 19, 25, 44, DateTimeKind.Utc)));
            // 34
            testsignals.Add(new TestSignalInfo("2022-01-06T191042Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                36, new DateTime(2022, 01, 06, 19, 10, 42, DateTimeKind.Utc)));
            // 35
            testsignals.Add(new TestSignalInfo("2022-01-06T200702Z, 264.200 kHz, Wide-U.wav", "White noise",
                0, new DateTime(2022, 01, 06, 20, 07, 02, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.Noise));
            // 36
            testsignals.Add(new TestSignalInfo("2022-01-06T200830Z, 193 kHz, Wide-U.wav", "BBC 4",
                27, new DateTime(2022, 01, 06, 20, 08, 30, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.BBC4_AMDS));
            // 37
            testsignals.Add(new TestSignalInfo("2022-01-06T201007Z, 72.500 kHz, Wide-U.wav", "DCF77",
                40, new DateTime(2022, 01, 06, 20, 10, 07, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.DCFp));
            // 38
            testsignals.Add(new TestSignalInfo("2022-01-06T201127Z, 14.700 kHz, Wide-U.wav", "VLF Data",
                0, new DateTime(2022, 01, 06, 20, 11, 27, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.Noise));
            // 39
            testsignals.Add(new TestSignalInfo("2022-01-07T080923Z, 157 kHz, Wide-U.wav", "Morning",
                28, new DateTime(2022, 01, 07, 08, 09, 23, DateTimeKind.Utc)));
            // 40 - Heppen.be recording
            testsignals.Add(new TestSignalInfo("websdr_recording_2022-01-07T08_20_10Z_157.0kHz.wav", "Heppen.be",
                40, new DateTime(2022, 01, 07, 08, 18, 23, DateTimeKind.Utc)));
            // 41 - Krizevci, Croatia - http://9a1cra.ddns.net:8902/
            testsignals.Add(new TestSignalInfo("websdr_recording_2022-01-07T08_25_11Z_157.0kHz.wav", "Croatia",
                30, new DateTime(2022, 01, 07, 08, 23, 23, DateTimeKind.Utc)));
            // 42 - Grimsby UK http://grimsbysdr.ddns.net:8073 - N/S
            // this recording is at a 1 kHz IF
            // there is also a frequency offset with this receiver of around 350 Hz
            testsignals.Add(new TestSignalInfo("websdr_recording_2022-01-07T08_37_08Z_161.0kHz.wav", "Grimsby",
                30, new DateTime(2022, 01, 07, 08, 35, 30, DateTimeKind.Utc), _frequency: 1350));
            // 43
            testsignals.Add(new TestSignalInfo("websdr_recording_start_2022-01-07T09_16_42Z_193.0kHz.wav", "BBC 4",
    70, new DateTime(2022, 01, 07, 09, 16, 42, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.BBC4_AMDS));
            // 44
            testsignals.Add(new TestSignalInfo("websdr_recording_start_2022-01-07T09_20_29Z_193.0kHz.wav", "BBC 4",
    70, new DateTime(2022, 01, 07, 09, 20, 29, DateTimeKind.Utc), _status: TestSignalInfo.Station_Status.Maintenance, _signaltype: TestSignalInfo.SignalTypeEnum.BBC4_AMDS));
            // 45
            testsignals.Add(new TestSignalInfo("2022-01-07T094204Z, 157 kHz, Wide-U.wav", "Morning",
                38, new DateTime(2022, 01, 07, 09, 42, 04, DateTimeKind.Utc)));
        }
    }
}
