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
            FM_Convolver_Biased
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
                    SearchFirstMin = 0.75,
                    SearchFirstMax = 1.2,
                    SearchRange = 1.05,
                    UseInitialZeroCorrection = true,
                    UseTemplateLengthCorrection = true,
                    UseDataInversion = false,
                    UseSymmetryWeight = false,
                    SymmetryWeightFactor = 0.2,
                    FIROffsetFactor = 0.2,
                    AutoThreshold = DemodulatorContext.AutoThresholdModes.None,
                    AutoThresholdMaxBias = 1.25,
                    UseFIROffset = false
                },
                CorrelatorParameters = new DemodulatorContext.CorrelatorParametersStruct()
                {
                    ZeroOffset = -28,
                    OneOffset = -24,
                    CorrelatorDataSource = DemodulatorContext.CorrelatorDataSourceTypes.FM,
                    CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Real
                },
                CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM,
                FilterParameters = new DemodulatorContext.FilterParametersStruct() { 
                    FMAverageCount = 8, 
                    IQAverageCount = 100, 
                    EnvelopeAverageCount = 64 
                },
                MinuteDetectorParameters = new DemodulatorContext.MinuteDetectorParametersStruct() { 
                    Convolver_Length = 512,
                    Weighting_Coefficient = 3
                },
                DemodulationResult = new DemodulatorContext.DemodulationResultStruct()
            };

            switch (demodulator)
            {
                case DemodulatorDefaults.FM:
                    break;
                case DemodulatorDefaults.FM_Biased:
                    demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Biased;
                    break;
                case DemodulatorDefaults.FM_Biased_MeanVariance:
                    demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Biased_MeanVariance;
                    demod.DataSlicerParameters.AutoThreshold = DemodulatorContext.AutoThresholdModes.MeanVariance;
                    demod.DataSlicerParameters.UseFIROffset = false;
                    demod.DataSlicerParameters.UseSymmetryWeight = true;
                    break;
                case DemodulatorDefaults.FM_Convolver_Biased:
                case DemodulatorDefaults.FM_Convolver:
                    if (demodulator == DemodulatorDefaults.FM_Convolver_Biased)
                        demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Convolve_Biased;
                    else
                        demod.CorrelatorType = DemodulatorContext.CorrelatorTypeEnum.FM_Convolve;

                    demod.CorrelatorParameters = new DemodulatorContext.CorrelatorParametersStruct()
                    {
                        // these parameters need a lookin' at
                        KernelLength = 512,
                        CommonOffset = 256 + 259,
                        ZeroOffset = 0,
                        OneOffset = 9,
                        CorrelatorReferenceSource = DemodulatorContext.CorrelatorReferenceSourceTypes.Synthetic
                    };
                    demod.DataSlicerParameters = new DemodulatorContext.DataSlicerParameterStruct()
                    {
                        BiasOffset = 0,
                        AutoBias_Level = 0.25,
                        Threshold = 1.25,
                        SearchFirstMin = 0.75,
                        SearchFirstMax = 1.2,
                        SearchRange = 1.05,
                        UseInitialZeroCorrection = true,
                        UseTemplateLengthCorrection = false,
                        UseSymmetryWeight = true,
                        SymmetryWeightFactor = 0.05,
                        UseFIROffset = true,
                        FIROffsetFactor = 0.01
                    };

                    break;
            }

            return demod;
        }
    }
}
