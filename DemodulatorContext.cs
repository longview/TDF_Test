using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    public class DemodulatorContext
    {
        public DemodulatorContext(CorrelatorTypeEnum correlatortype)
        {
            CorrelatorType = correlatortype;
        }

        public MinuteDetectorTypeEnum MinuteDetectorType { get; set; } = MinuteDetectorTypeEnum.Convolver;
        public MinuteDetectorParametersStruct MinuteDetectorParameters;
        public CorrelatorParametersStruct CorrelatorParameters;
        public CorrelatorTypeEnum CorrelatorType { get; set; } = CorrelatorTypeEnum.FM;
        public DataSlicerParameterStruct DataSlicerParameters { get; set; }
        public FilterParametersStruct FilterParameters { get; set; }

        //public bool[] DemodulatedData;
        public DemodulationResultStruct DemodulationResult;

        public double DecimatedSamplePeriod;

        public DataSlicerResultStruct DataSlicerResults;


        public struct DemodulationResultStruct
        {
            public bool[] DemodulatedData;
            public bool[] DemodulatedDataReference;
            public bool[] DemodulatedDataErrorMask;
            public string DemodulatedDataErrorDescription;
            public int BitErrors;
            public int DecodeErrors;
        }

        public enum CorrelatorTypeEnum
        {
            FM,
            FM_Biased,
            PM,
            FM_Convolve,
            FM_Convolve_Biased,
            PM_Convolve,
            PM_Convolve_Biased
        }

        public enum MinuteDetectorTypeEnum
        {
            Convolver
        }

        public struct CorrelatorParametersStruct
        {
            public int ZeroOffset { get; set; }
            public int OneOffset { get; set; }
            public int CommonOffset { get; set; }
            public int KernelLength { get; set; }
            public CorrelatorReferenceSourceTypes CorrelatorReferenceSource { get; set; }
            public CorrelatorDataSourceTypes CorrelatorDataSource { get; set; }
            public double[] DemodulatorSource;
            public double[] ZeroDemodulatorResult;
            public double[] OneDemodulatorResult;
            public double[] ZeroCorrelatorReference;
            public double[] OneCorrelatorReference;
            public bool TimeReverseCorrelators;
        }

        public enum CorrelatorDataSourceTypes
        {
            FM,
            PM,
            FM_Envelope,
            PM_Envelope
        }

        public struct MinuteDetectorParametersStruct
        {
            public int Convolver_Length;
            public double Weighting_Coefficient;
            public double[] MinuteDetectorSource;
            public double[] MinuteDetectorWeightedOutput;
            public double[] MinuteDetectorCorrelationOutput;
            public int MinuteDetectorResult;
        }

        public enum CorrelatorReferenceSourceTypes
        {
            Real,
            Synthetic,
        }




        public struct FilterParametersStruct
        {
            public int IQAverageCount { get; set; }
            public int FMAverageCount { get; set; }
            public int EnvelopeAverageCount { get; set; }
        }


        public struct DataSlicerParameterStruct
        {
            public double BiasOffset { get; set; }
            public double Threshold { get; set; }
            public double AutoBias_Level { get; set; }
            public double SearchFirstMin { get; set; }
            public double SearchFirstMax { get; set; }
            public double SearchRange { get; set; }
            public bool UseInitialZeroCorrection { get; set; }
            public bool UseTemplateLengthCorrection { get; set; }
            public bool UseDataInversion { get; set; }
            public bool UseSymmetryWeight { get; set; }
            public double SymmetryWeightFactor { get; set; }
            public bool UseFIROffset { get; set; }
            public double FIROffsetFactor { get; set; }
        }

        public struct DataSlicerResultStruct
        {
            public double[] SecondSampleTimes;
            public double[] SecondSampleRatios;
            public double[] OnePeaks;
            public double[] ZeroPeaks;
            public double[] OneWeightedPeaks;
            public double[] ZeroWeightedPeaks;
        }




        public override string ToString()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM:
                    return "FM";
                case CorrelatorTypeEnum.FM_Biased:
                    return "FM with bias";
                case CorrelatorTypeEnum.PM:
                    return "PM";
                case CorrelatorTypeEnum.FM_Convolve:
                    return "FM convolver";
                case CorrelatorTypeEnum.FM_Convolve_Biased:
                    return "FM convolver with bias";
                case CorrelatorTypeEnum.PM_Convolve:
                    return "PM convolver";
                case CorrelatorTypeEnum.PM_Convolve_Biased:
                    return "PM convolver with bias";
                default:
                    return "Unknown";
            }
        }

        public bool IsConvolver()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM_Convolve:
                case CorrelatorTypeEnum.FM_Convolve_Biased:
                case CorrelatorTypeEnum.PM_Convolve:
                case CorrelatorTypeEnum.PM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

        public bool UsesPM()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.PM:
                case CorrelatorTypeEnum.PM_Convolve:
                case CorrelatorTypeEnum.PM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

        public bool UsesFM()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM:
                case CorrelatorTypeEnum.FM_Convolve:
                case CorrelatorTypeEnum.FM_Biased:
                case CorrelatorTypeEnum.FM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsBiased()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM_Biased:
                case CorrelatorTypeEnum.FM_Convolve_Biased:
                case CorrelatorTypeEnum.PM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

    }
}
