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
        public DataSlicerParameterStruct DataSlicerParameters;
        public FilterParametersStruct FilterParameters { get; set; }

        public DemodulationResultStruct DemodulationResult;

        public double DecimatedSamplePeriod;

        public DataSlicerResultStruct DataSlicerResults;

        public enum AutoThresholdModes
        {
            None,
            Mean,
            MeanVariance
        }

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
            FM_Biased_MeanVariance,
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
            public int SyntheticCorrelatorAverageCount;
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
            public AutoThresholdModes AutoThreshold { get; set; }
            public double AutoThresholdMaxBias { get; set; }
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

        public string ToLongString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Description of demodulator: ");
            sb.AppendLine(this.ToString());

            sb.AppendFormat("Filter parameters: IQ {0}, FM {1}, Rectified {2}\r\n", FilterParameters.IQAverageCount, FilterParameters.FMAverageCount, FilterParameters.EnvelopeAverageCount);
            sb.AppendFormat("Minute detector type {0}, convolve length {1}, weight factor {3}, found at sample {2}\r\n", "Convolve", MinuteDetectorParameters.Convolver_Length, MinuteDetectorParameters.MinuteDetectorResult,
                MinuteDetectorParameters.Weighting_Coefficient);
            sb.AppendFormat("Correlator input {0}, {1} reference, kernel {2}, offset {3}, 0:{4} 1:{5}, reversed: {6}, synth corrs average {7}\r\n", CorrelatorParameters.CorrelatorDataSource == CorrelatorDataSourceTypes.FM ? "FM":"PM",
                CorrelatorParameters.CorrelatorReferenceSource == CorrelatorReferenceSourceTypes.Real ? "real":"synthetic",
                CorrelatorParameters.KernelLength, CorrelatorParameters.CommonOffset, CorrelatorParameters.ZeroOffset, CorrelatorParameters.OneOffset,
                CorrelatorParameters.TimeReverseCorrelators, CorrelatorParameters.SyntheticCorrelatorAverageCount);
            sb.AppendFormat("Data slicer bias offset {0:F3}, thres. {1}, autobias level {2}, start {3}, stop {4}, increment {5}, initial zero correct {6}, template length correct {7}, data inverted {8}, symmetry weighted {9}, symmetry weight scale {10}, FIR offset {11}, FIR offset scale {12}, autothreshold max bias {13}\r\n",
                DataSlicerParameters.BiasOffset, DataSlicerParameters.Threshold, DataSlicerParameters.AutoBias_Level, DataSlicerParameters.SearchFirstMin, DataSlicerParameters.SearchFirstMax, DataSlicerParameters.SearchRange, DataSlicerParameters.UseInitialZeroCorrection,
                DataSlicerParameters.UseTemplateLengthCorrection, DataSlicerParameters.UseDataInversion, DataSlicerParameters.UseSymmetryWeight, DataSlicerParameters.SymmetryWeightFactor, DataSlicerParameters.UseFIROffset,
                DataSlicerParameters.UseFIROffset, DataSlicerParameters.FIROffsetFactor, DataSlicerParameters.AutoThresholdMaxBias);




            return sb.ToString();
        }


        public override string ToString()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM:
                    return "FM";
                case CorrelatorTypeEnum.FM_Biased:
                    return "FM with bias";
                case CorrelatorTypeEnum.FM_Biased_MeanVariance:
                    return "FM with bias and mean-variance autothresholds";
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
                case CorrelatorTypeEnum.FM_Biased_MeanVariance:
                case CorrelatorTypeEnum.FM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsFMBaseType()
        {
            switch (CorrelatorType)
            {
                case CorrelatorTypeEnum.FM:
                case CorrelatorTypeEnum.FM_Biased:
                case CorrelatorTypeEnum.FM_Biased_MeanVariance:
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
                case CorrelatorTypeEnum.FM_Biased_MeanVariance:
                case CorrelatorTypeEnum.PM_Convolve_Biased:
                    return true;
                default:
                    return false;
            }
        }

    }
}
