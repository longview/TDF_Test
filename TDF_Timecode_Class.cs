using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    /*  A class to represent a timecode from a bitstream, or to generate a new timecode for simulation purposes.
     * 
     */
    class TDF_Timecode_Class
    {
        public TDF_Timecode_Class(DateTime time, bool summertime = false, 
            bool holidaytomorrow = false, bool holidaytoday = false, 
            LeapSecondState leapstate = LeapSecondState.No_Leap)
        {
            _bits = new bool[59];
            _bit_errors = new bool[59];
            Summertime_Announced = summertime;

            Comparison_Error_Description = "No comparison performed yet.";

            if (!SetCurrentTransmittedTimeAndTimezone(time))
                throw new MissingFieldException("Timezone must be of kind Local or UTC");
            Tomorrow_Is_Holiday = holidaytomorrow;
            Today_Is_Holiday = holidaytoday;
            _leapstate = leapstate;

            // call this to update our internal store
            GetBitstream();
    }

        public int GetBitstreamErrorCount()
        {
            int errorcount = 0;
            // then count up the set bits
            for (int i = 0; i < 59; i++)
            {
                // bit 15 is special and can be whatever
                if (i == 15)
                    continue;
                errorcount += _bit_errors[i] ? 1 : 0;
            }
            return errorcount;
        }

        // returns an array of indices to where bit errors were found
        public int[] GetBitstreamBitErrorPositions()
        {
            _bit_error_positions = new List<int>(59);
            for (int i = 0; i < 59; i++)
            {
                // bit 15 is special and can be whatever
                if (i == 15)
                    continue;
                _bit_error_positions.Add(i);
            }

            return _bit_error_positions.ToArray();
        }

        public bool[] GetBitstreamErrorMask()
        {
            return _bit_errors;
        }

        public enum LeapSecondState
        {
            No_Leap,
            Positive_Leap,
            Negative_Leap
        }

        public DateTime GetCurrentTransmittedTime()
        {
            return Current_Reported_Time;
        }

        public bool SetCurrentTransmittedTimeAndTimezone(DateTime time)
        {
            // can't deal with this one, be explicit
            if (time.Kind == DateTimeKind.Unspecified)
                return false;
            
            if (time.Kind == DateTimeKind.Local)
            {
                Current_Reported_Time = time;
            }
            else if (time.Kind == DateTimeKind.Utc)
            {
                Current_Reported_Time = time.ToLocalTime();
            }

            if (time.IsDaylightSavingTime())
                Timezone = TDF_Timezone.CEST;
            else
                Timezone = TDF_Timezone.CET;

            return true;
        }

        // compare bitstream with internally generated and report bit error count
        public int CompareBitstream (bool[] input)
        {
            if (input.Length != 59)
                throw new ArgumentException("Bitstream length is not 59");
                        
            // first do a simple XOR check:
            for(int i = 0; i < 59; i++)
                _bit_errors[i] = _bits[i] ^ input[i];


            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\r\nReference generator check:\r\nTotal bit errors found: {0}\r\n", GetBitstreamErrorCount());
            for (int i = 0; i < 59; i++)
            {
                if (!_bit_errors[i])
                    continue;

                sb.AppendFormat("[{0}] {1,5} should be {2,5}, \"{3}\"\r\n", i, input[i], _bits[i],
                    BitPosition_ToString((BitPositions)i));
            }

            sb.AppendLine();

            Comparison_Error_Description = sb.ToString();

            // the error count already has a function
            return GetBitstreamErrorCount();
        }

        public bool[] GetBitstream()
        {
            // generate a valid bitstream based on this objects state
            List<bool> bitgenerator = new List<bool>(59);

            bitgenerator.Add(false);
            bitgenerator.Add(_leapstate == LeapSecondState.Positive_Leap ? true : false);
            bitgenerator.Add(_leapstate == LeapSecondState.Negative_Leap ? true : false);
            // Hamming weights computed at the end
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);

            // unused bits are always 0
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);
            bitgenerator.Add(false);

            bitgenerator.Add(Tomorrow_Is_Holiday);
            bitgenerator.Add(Today_Is_Holiday);

            // to be ignored
            bitgenerator.Add(false);

            bitgenerator.Add(Summertime_Announced);
            bitgenerator.Add(Timezone == TDF_Timezone.CEST ? true : false);
            bitgenerator.Add(Timezone == TDF_Timezone.CET ? true : false);

            bitgenerator.Add(false);
            // S bit
            bitgenerator.Add(true);
            // generate the minutes
            byte[] minutes = ToBcd(Current_Reported_Time.Minute);
            bitgenerator.Add((minutes[0] & 1) > 0);
            bitgenerator.Add((minutes[0] & 2) > 0);
            bitgenerator.Add((minutes[0] & 4) > 0);
            bitgenerator.Add((minutes[0] & 8) > 0);
            bitgenerator.Add((minutes[1] & 1) > 0);
            bitgenerator.Add((minutes[1] & 2) > 0);
            bitgenerator.Add((minutes[1] & 4) > 0);

            int paritycount = 0;
            for (int i = 21; i < 28; i++)
            {
                if (bitgenerator[i])
                    paritycount++;
            }
            // P1 bit
            bitgenerator.Add(paritycount % 2 == 1);

            // generate the hours
            byte[] hours = ToBcd(Current_Reported_Time.Hour);
            bitgenerator.Add((hours[0] & 1) > 0);
            bitgenerator.Add((hours[0] & 2) > 0);
            bitgenerator.Add((hours[0] & 4) > 0);
            bitgenerator.Add((hours[0] & 8) > 0);
            bitgenerator.Add((hours[1] & 1) > 0);
            bitgenerator.Add((hours[1] & 2) > 0);

            paritycount = 0;
            for (int i = 29; i < 35; i++)
            {
                if (bitgenerator[i])
                    paritycount++;
            }
            // P2 bit
            bitgenerator.Add(paritycount % 2 == 1);

            // generate DoM
            byte[] DoM = ToBcd(Current_Reported_Time.Day);
            bitgenerator.Add((DoM[0] & 1) > 0);
            bitgenerator.Add((DoM[0] & 2) > 0);
            bitgenerator.Add((DoM[0] & 4) > 0);
            bitgenerator.Add((DoM[0] & 8) > 0);
            bitgenerator.Add((DoM[1] & 1) > 0);
            bitgenerator.Add((DoM[1] & 2) > 0);

            // generate DoW
            byte[] DoW = ToBcd((int)Current_Reported_Time.DayOfWeek);
            bitgenerator.Add((DoW[0] & 1) > 0);
            bitgenerator.Add((DoW[0] & 2) > 0);
            bitgenerator.Add((DoW[0] & 4) > 0);

            // generate Month
            byte[] Month = ToBcd(Current_Reported_Time.Month);
            bitgenerator.Add((Month[0] & 1) > 0);
            bitgenerator.Add((Month[0] & 2) > 0);
            bitgenerator.Add((Month[0] & 4) > 0);
            bitgenerator.Add((Month[0] & 8) > 0);
            bitgenerator.Add((Month[1] & 1) > 0);

            // generate Year
            byte[] Year = ToBcd(Current_Reported_Time.Year - 2000);
            bitgenerator.Add((Year[0] & 1) > 0);
            bitgenerator.Add((Year[0] & 2) > 0);
            bitgenerator.Add((Year[0] & 4) > 0);
            bitgenerator.Add((Year[0] & 8) > 0);
            bitgenerator.Add((Year[1] & 1) > 0);
            bitgenerator.Add((Year[1] & 2) > 0);
            bitgenerator.Add((Year[1] & 4) > 0);
            bitgenerator.Add((Year[1] & 8) > 0);


            paritycount = 0;
            for (int i = 36; i < 58; i++)
            {
                if (bitgenerator[i])
                    paritycount++;
            }

            bitgenerator.Add(paritycount % 2 == 1);

            byte hammingcount = 0;
            for (int i = 21; i < 59; i++)
            {
                if (bitgenerator[i])
                    hammingcount++;
            }

            bitgenerator[3] = (hammingcount & 2) > 0;
            bitgenerator[4] = (hammingcount & 4) > 0;
            bitgenerator[5] = (hammingcount & 8) > 0;
            bitgenerator[6] = (hammingcount & 16) > 0;

            _bits = bitgenerator.ToArray();

            return _bits;
        }
        
        public enum TDF_Timezone
        {
            CET,
            CEST
        }

        public string Comparison_Error_Description;

        public bool Summertime_Announced;
        // the time we will be transmitting, not time at time of transmission
        public DateTime Current_Reported_Time;
        public TDF_Timezone Timezone;

        public bool Tomorrow_Is_Holiday;
        public bool Today_Is_Holiday;
        public LeapSecondState _leapstate;
        // the current bitstream representation of the state
        private bool[] _bits;
        // bitmask of errors in bitstream
        private bool[] _bit_errors;

        private List<int> _bit_error_positions;

        // Names for all bits in bitstream
        public enum BitPositions
        {
            M,A2,A3,
            HA02,HA04,HA08,HA16,
            NULL7,NULL8,NULL9,NULL10,NULL11,NULL12,
            F1,F2,IGNORED15,A1,Z1,Z2,UNUSED19,S,
            MIN01,MIN02,MIN04,MIN08,MIN10,MIN20,MIN40,P1,
            H01,H02,H04,H08,H10,H20,P2,
            DOM01,DOM02,DOM04,DOM08,DOM10,DOM20,
            DOW01,DOW02,DOW04,
            MON01,MON02,MON04,MON08,MON10,
            Y01,Y02,Y04,Y08,Y10,Y20,Y40,Y80,P3
        }

        // printable names for all bits in bitstream
        public static string BitPosition_ToString(BitPositions bit)
        {
            switch (bit)
            {
                case BitPositions.A1:
                    return "A1 Summer time announcement, withtin 1 hour";
                case BitPositions.A2:
                    return "A2 Positive leap warning";
                case BitPositions.A3:
                    return "A3 Negative leap warning";
                case BitPositions.DOM01:
                    return "Day of Month, 1";
                case BitPositions.DOM02:
                    return "Day of Month, 2";
                case BitPositions.DOM04:
                    return "Day of Month, 4";
                case BitPositions.DOM08:
                    return "Day of Month, 8";
                case BitPositions.DOM10:
                    return "Day of Month, 10";
                case BitPositions.DOM20:
                    return "Day of Month, 20";
                case BitPositions.DOW01:
                    return "Day of Week, 1";
                case BitPositions.DOW02:
                    return "Day of Week, 2";
                case BitPositions.DOW04:
                    return "Day of Week, 4";
                case BitPositions.F1:
                    return "F1 Following day is a public holiday";
                case BitPositions.F2:
                    return "F2 Today is a public holiday";
                case BitPositions.H01:
                    return "Hours, 1";
                case BitPositions.H02:
                    return "Hours, 2";
                case BitPositions.H04:
                    return "Hours, 4";
                case BitPositions.H08:
                    return "Hours, 8";
                case BitPositions.H10:
                    return "Hours, 10";
                case BitPositions.H20:
                    return "Hours, 20";
                case BitPositions.HA02:
                    return "Hamming weight 21-58, 2";
                case BitPositions.HA04:
                    return "Hamming weight 21-58, 4";
                case BitPositions.HA08:
                    return "Hamming weight 21-58, 8";
                case BitPositions.HA16:
                    return "Hamming weight 21-58, 16";
                case BitPositions.IGNORED15:
                    return "Ignored";
                case BitPositions.M:
                    return "M Start of minute";
                case BitPositions.MIN01:
                    return "Minutes, 1";
                case BitPositions.MIN02:
                    return "Minutes, 2";
                case BitPositions.MIN04:
                    return "Minutes, 4";
                case BitPositions.MIN08:
                    return "Minutes, 8";
                case BitPositions.MIN10:
                    return "Minutes, 10";
                case BitPositions.MIN20:
                    return "Minutes, 20";
                case BitPositions.MIN40:
                    return "Minutes, 40";
                case BitPositions.MON01:
                    return "Month, 1";
                case BitPositions.MON02:
                    return "Month, 2";
                case BitPositions.MON04:
                    return "Month, 4";
                case BitPositions.MON08:
                    return "Month, 8";
                case BitPositions.MON10:
                    return "Month, 10";
                case BitPositions.NULL10:
                case BitPositions.NULL11:
                case BitPositions.NULL12:
                case BitPositions.NULL7:
                case BitPositions.NULL8:
                case BitPositions.NULL9:
                case BitPositions.UNUSED19:
                    return "Unused, always 0";
                case BitPositions.P1:
                    return "P1 Even parity, minutes";
                case BitPositions.P2:
                    return "P2 Even parity, hours";
                case BitPositions.P3:
                    return " P3 Even parity, DoM, DoW, Mon, Year";
                case BitPositions.Y01:
                    return "Year, 1";
                case BitPositions.Y02:
                    return "Year, 2";
                case BitPositions.Y04:
                    return "Year, 4";
                case BitPositions.Y08:
                    return "Year, 8";
                case BitPositions.Y10:
                    return "Year, 10";
                case BitPositions.Y20:
                    return "Year, 20";
                case BitPositions.Y40:
                    return "Year, 40";
                case BitPositions.Y80:
                    return "Year, 80";
                case BitPositions.Z1:
                    return "Z1 CEST (summer time)";
                case BitPositions.Z2:
                    return "Z2 CET (standard time)";
                default:
                    return "Error, unknown bit position.";

            }
        }

        // https://stackoverflow.com/questions/2448303/converting-a-int-to-a-bcd-byte-array
        // but didn't seem to work so rewrite
        private byte[] ToBcd(int value)
        {
            if (value < 0 || value > 99999999)
                throw new ArgumentOutOfRangeException("value");
            byte[] ret = new byte[4];
            while (value >= 10)
            {
                ret[1] += 10;
                value -= 10;
            }
            ret[0] = (byte)value;
            ret[1] /= 10;
            /*for (int i = 0; i < 4; i++)
            {
                ret[i] = (byte)(value % 10);
                value /= 10;
                ret[i] |= (byte)((value % 10) << 4);
                value /= 10;
            }*/
            return ret;
        }
    }
}
