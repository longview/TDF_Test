﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    public struct TestSignalInfo
    {
        public TestSignalInfo(string _filepath, string _comment, double _snr, DateTime _date, bool holidaytoday = false, bool holidaytomorrow = false, bool summertime_soon = false, double _frequency = 5000, int _errors = 0, Station_Status _status = Station_Status.OnAir, Signal_Type _signaltype = Signal_Type.TDF)
        {
            FilePath = _filepath;
            Comment = _comment;
            SNR = _snr;
            Frequency = _frequency;
            Status = _status;

            // store where we expect to find the start of the next minute
            ExpectedMinuteStartSeconds = 60 - _date.Second;
            // add 1 minute to timestamp from start of recording timestamp
            // and remove the seconds
            RecordedTimestampUTC = _date.AddMinutes(2).AddSeconds(_date.Second * -1);
            Reference_Timecode = new TDF_Timecode_Class(RecordedTimestampUTC, summertime_soon, holidaytomorrow, holidaytoday);
            SignalType = _signaltype;
            ExpectedErrors = _errors;
        }
        public string FilePath;
        public string Comment;
        public double Frequency;
        public double SNR;
        public Station_Status Status;
        public DateTime RecordedTimestampUTC;
        public Signal_Type SignalType;
        public int ExpectedErrors;
        public double ExpectedMinuteStartSeconds;

        public TDF_Timecode_Class Reference_Timecode;
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
}