using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDF_Test
{
    partial class Program
    {
        public enum DemodulatorDefaults
        {
            FM,
            FM_Biased,
            FM_Biased_MeanVariance,
            FM_Convolver,
            FM_Convolver_Biased,
            FM_Convolver_Biased_MeanVariance,
            PM,
            PM_Biased,
            PM_Biased_MeanVariance
        }

        public static DemodulatorContext.CorrelatorParametersStruct GetCorrelationParameter_SSAD()
        {
            return new DemodulatorContext.CorrelatorParametersStruct()
            {
                ZeroOffset = -18,
                OneOffset = -14,
                CorrelatorDataSource = DemodulatorContext.CorrelatorDataSourceTypes.FM,
                CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Real,
                UseAverageSubtraction = false,
                UseOutputHighPassFiltering = true,
                OutputHighPassFilterCoefficient = 0.8,
                UseInputHighPassFiltering = true,
                InputHighPassFilterCoefficient = 0.98,
                // note: if this is tuend off, the time offsets must be changed by around -10
                TimeReverseCorrelators = true,
                ResultScaling = -1,
                CorrelatorMethod = DemodulatorContext.CorrelatorMethodEnum.SSAD
            };
        }

        public static DemodulatorContext.CorrelatorParametersStruct GetCorrelationParameter_MAC()
        {
            return new DemodulatorContext.CorrelatorParametersStruct()
            {
                CommonOffset = -8,
                ZeroOffset = -18,
                OneOffset = 2,
                CorrelatorDataSource = DemodulatorContext.CorrelatorDataSourceTypes.FM,
                CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Synthetic,
                UseAverageSubtraction = false,
                UseOutputHighPassFiltering = true,
                OutputHighPassFilterCoefficient = 0.8,
                UseInputHighPassFiltering = true,
                InputHighPassFilterCoefficient = 0.98,
                // note: if this is tuend off, the time offsets must be changed by around -10
                TimeReverseCorrelators = true,
                ResultScaling = -1,
                CorrelatorMethod = DemodulatorContext.CorrelatorMethodEnum.MAC
            };
        }

        public static DemodulatorContext.CorrelatorParametersStruct GetCorrelationParameter_SSAD_MAC()
        {
            return new DemodulatorContext.CorrelatorParametersStruct()
            {
                CommonOffset = -8,
                ZeroOffset = -18,
                OneOffset = 2,
                CorrelatorDataSource = DemodulatorContext.CorrelatorDataSourceTypes.FM,
                CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Synthetic,
                UseAverageSubtraction = false,
                UseOutputHighPassFiltering = true,
                OutputHighPassFilterCoefficient = 0.8,
                UseInputHighPassFiltering = true,
                InputHighPassFilterCoefficient = 0.98,
                // note: if this is tuend off, the time offsets must be changed by around -10
                TimeReverseCorrelators = true,
                CorrelatorMethod = DemodulatorContext.CorrelatorMethodEnum.SSAD_MAC
            };
        }

        /* This is where the default parameters for each standard demodulator type is defined
         * 
         */
        public static DemodulatorContext GenerateDemodulator(DemodulatorDefaults demodulator)
        {
            // base type is FM standard
            DemodulatorContext demod = new DemodulatorContext(DemodulatorContext.CorrelatorTypeEnum.FM)
            {
                DataSlicerParameters = new DemodulatorContext.DataSlicerParameterStruct()
                {
                    AutoBias_Level = 0.25,
                    BiasOffset = -0.1,
                    Threshold = 1,
                    SearchFirstMin = 0.8,// these ranges can be tightened if the minute detector is real good
                    SearchFirstMax = 1.08, 
                    SearchRange = 1.05, // 1.05 is +-10 samples; this matches the correlation waveform quite well and seems optimal
                    UseIntegralTimeDelta = false, // true: search window is referred to the first second for the entire minute, this reduces the average time delta, but assumes first peak is good
                    UseInitialZeroCorrection = true,
                    UseTemplateLengthCorrection = true,
                    UseDataInversion = false,
                    UseSymmetryWeight = false, // look for a symmetrical gaussian detector pulse and weight by it. Seems to increase noise sensitivity.
                    SymmetryWeightFactor = 0.1,
                    AutoThreshold = DemodulatorContext.AutoThresholdModes.None,
                    AutoThresholdMaxBias = 1.25,
                    UseFIROffset = false, // weight result by values +-length of the correlator
                    FIROffsetFactor = 0.25,
                    UseCalibrateAllBits = false, // enabling this will remove around 10 potential bit errors by forcing all known 0 bits to 0 and recalibration the detector ratio
                },
                CorrelatorParameters = GetCorrelationParameter_SSAD(),
                CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM,
                FilterParameters = new DemodulatorContext.FilterParametersStruct() { 
                    FMAverageCount = 8, 
                    IQAverageCount = 100, 
                    EnvelopeAverageCount = 64 
                },
                MinuteDetectorParameters = new DemodulatorContext.MinuteDetectorParametersStruct() { 
                    Convolver_Length = 512,
                    Weighting_Coefficient = 3,
                    ResultOffset = 50,
                    Type = DemodulatorContext.MinuteDetectorTypeEnum.Correlator,
                },
                DemodulationResult = new DemodulatorContext.DemodulationResultStruct()
            };

            switch (demodulator)
            {
                case DemodulatorDefaults.FM:
                    break;
                case DemodulatorDefaults.FM_Biased:
                    demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Biased;
                    demod.DataSlicerParameters.BiasOffset = -0.15;
                    demod.DataSlicerParameters.UseFIROffset = true; // this improves performance for this detector.
                    break;
                case DemodulatorDefaults.FM_Biased_MeanVariance:
                    demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Biased_MeanVariance;
                    demod.DataSlicerParameters.AutoThreshold = DemodulatorContext.AutoThresholdModes.MeanVariance;
                    demod.DataSlicerParameters.UseFIROffset = false;
                    demod.DataSlicerParameters.UseSymmetryWeight = true;
                    demod.DataSlicerParameters.BiasOffset = -0.15;
                    break;
                case DemodulatorDefaults.PM_Biased_MeanVariance:
                case DemodulatorDefaults.PM_Biased:
                    demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.PM_Biased;
                    demod.CorrelatorParameters = GetCorrelationParameter_MAC();
                    demod.CorrelatorParameters.CorrelatorDataSource = DemodulatorContext.CorrelatorDataSourceTypes.PM;
                    demod.CorrelatorParameters.TimeReverseCorrelators = true;
                    demod.CorrelatorParameters.UseInvertResult = true;
                    demod.CorrelatorParameters.CommonOffset = -13;
                    demod.CorrelatorParameters.ZeroOffset = -18;
                    demod.CorrelatorParameters.OneOffset = -10;

                    if (demodulator == DemodulatorDefaults.PM_Biased_MeanVariance)
                        demod.DataSlicerParameters.AutoThreshold = DemodulatorContext.AutoThresholdModes.MeanVariance;
                    break;
                case DemodulatorDefaults.FM_Convolver_Biased:
                case DemodulatorDefaults.FM_Convolver_Biased_MeanVariance:
                case DemodulatorDefaults.FM_Convolver:
                    if (demodulator == DemodulatorDefaults.FM_Convolver_Biased)
                        demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Convolve_Biased;
                    else
                        demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Convolve;

                    if (demodulator == DemodulatorDefaults.FM_Convolver_Biased_MeanVariance)
                        demod.DataSlicerParameters.AutoThreshold = DemodulatorContext.AutoThresholdModes.MeanVariance;

                    demod.CorrelatorParameters = new DemodulatorContext.CorrelatorParametersStruct()
                    {
                        // these parameters need a lookin' at
                        KernelLength = 512,
                        CommonOffset = 256 + 259,
                        ZeroOffset = 0,
                        OneOffset = 9,
                        CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Synthetic,
                        UseAverageSubtraction = false,
                        UseOutputHighPassFiltering = true,
                        OutputHighPassFilterCoefficient = 0.8,
                        CorrelatorMethod = DemodulatorContext.CorrelatorMethodEnum.Convolution,
                    };
                    demod.DataSlicerParameters = new DemodulatorContext.DataSlicerParameterStruct()
                {
                    AutoBias_Level = 0.25,
                    BiasOffset = -0.1,
                    Threshold = 1,
                    SearchFirstMin = 0.8,
                    SearchFirstMax = 1.2,
                    SearchRange = 1.05,
                    UseInitialZeroCorrection = true,
                    UseTemplateLengthCorrection = false,
                    UseDataInversion = false,
                    UseSymmetryWeight = true,
                    SymmetryWeightFactor = 0.1,
                    FIROffsetFactor = 0.2,
                    AutoThreshold = DemodulatorContext.AutoThresholdModes.None,
                    AutoThresholdMaxBias = 1.25,
                    UseFIROffset = false,
                    UseCalibrateAllBits = false,
                };

                    break;
            }

            return demod;
        }
    }
}
