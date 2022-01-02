using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    partial class Program
    {

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
    }
}
