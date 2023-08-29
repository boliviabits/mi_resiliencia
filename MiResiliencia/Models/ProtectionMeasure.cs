using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ProtectionMeasure
    {
        [Key]
        [TableIgnore]
        public int ID { get; set; }

        [Display(ResourceType = typeof(Resources.Global), Name = "ConstructionCosts")]
        [LocalizedDisplayName(nameof(ResModel.PM_Costs), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public int Costs { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "LifeSpan")]
        [LocalizedDisplayName(nameof(ResModel.PM_LifeSpan), typeof(ResModel))]
        public int LifeSpan { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "OperatingCosts")]
        [LocalizedDisplayName(nameof(ResModel.PM_OperatingCosts), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public int OperatingCosts { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "MaintenanceCosts")]
        [LocalizedDisplayName(nameof(ResModel.PM_MaintenanceCosts), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public int MaintenanceCosts { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "RateofReturn")]
        [LocalizedDisplayName(nameof(ResModel.PM_RateOfReturn), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_PercentF3), typeof(ResFormat))]
        public double RateOfReturn { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Description")]
        [LocalizedDisplayName(nameof(ResModel.PM_Description), typeof(ResModel))]
        public string? Description { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "ValueAddedTax")]
        [LocalizedDisplayName(nameof(ResModel.PM_ValueAddedTax), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_PercentF3), typeof(ResFormat))]
        [TableIgnore]
        public double ValueAddedTax { get; set; }
        [TableIgnore]
        public int ProjectID { get; set; }
        [TableIgnore]
        public MultiPolygon? geometry { get; set; }
        [TableIgnore]
        public virtual Project Project { get; set; }
        [TableIgnore]
        [Display(ResourceType = typeof(Resources.Global), Name = "DiscountRate")]
        public double DiscountRate { get; set; }
        

        //---------------------------------------------------------------------------------
        //not in db

        [LocalizedDisplayName(nameof(ResModel.PM_YearlyCosts), typeof(ResModel))]
        //[DisplayFormat(DataFormatString = "{0:C}/a")]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public virtual double YearlyCosts
        {
            get
            {
                if (LifeSpan < 1)
                {
                    return 0.0d;
                }

                double _result = (1.0d + ValueAddedTax / 100.0d) * (
                    (double)OperatingCosts +
                    (double)MaintenanceCosts +
                    ((double)Costs - 0.0d) / (double)LifeSpan +
                    ((double)Costs + 0.0d) * RateOfReturn / 2.0d / 100.0d
                    );

                return _result;
            }
        }

        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.PM_LogYearlyCosts), typeof(ResModel))]
        public virtual string LogYearlyCosts
        {
            get
            {
                string _result =
                    $"YearlyCosts = (1 + ValueAddedTax / 100) * [" +
                    $"OperatingCosts + " +
                    $"MaintenanceCosts + " +
                    $"(ConstructionCosts - 0) / LifeSpan + " +
                    $"(ConstructionCosts + 0) * RateOfReturn / 2 / 100 ] ; \n";

                _result +=
                    $"YearlyCosts = (1 + {ValueAddedTax:F3} / 100) * [" +
                    $"{(double)OperatingCosts:F0} + " +
                    $"{(double)MaintenanceCosts:F0} + " +
                    $"({(double)Costs:F0} - 0) / {(double)LifeSpan:F0} + " +
                    $"({(double)Costs:F0} + 0) * {RateOfReturn:F3} / 2 / 100 ] ";
                ;

                if (LifeSpan < 1)
                {
                    _result += $"\nERROR: LifeSpan = {LifeSpan} years";
                }

                return _result;
            }
        }
    }
}