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
                22, new DateTime(2021, 12, 31, 18, 13, 22, DateTimeKind.Utc), _errors: 33, holidaytomorrow: true));
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
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T155905Z, 157 kHz, Wide-U.wav", "Good signal, afternoon",
                34, new DateTime(2022, 01, 02, 15, 59, 05, DateTimeKind.Utc)));
            // 23
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T172940Z, 157 kHz, Wide-U.wav", "Good signal, early evening",
                45, new DateTime(2022, 01, 02, 17, 29, 40, DateTimeKind.Utc)));
            // 24
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T185525Z, 157 kHz, Wide-U.wav", "Near perfect, evening",
                55, new DateTime(2022, 01, 02, 18, 55, 25, DateTimeKind.Utc)));
            // 25
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T193314Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                45, new DateTime(2022, 01, 02, 19, 33, 14, DateTimeKind.Utc)));
            // 26
            testsignals.Add(new TestSignalInfo("..\\..\\2022-01-02T194820Z, 157 kHz, Wide-U.wav", "Good signal, evening",
                44, new DateTime(2022, 01, 02, 19, 48, 20, DateTimeKind.Utc)));
        }
    }
}
