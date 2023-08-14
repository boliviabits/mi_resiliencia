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
        public int Costs { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "LifeSpan")]
        public int LifeSpan { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "OperatingCosts")]
        public int OperatingCosts { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "MaintenanceCosts")]
        public int MaintenanceCosts { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "RateofReturn")]
        public double RateOfReturn { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Description")]
        public string? Description { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "ValueAddedTax")]
        public double ValueAddedTax { get; set; }
        public int ProjectID { get; set; }
        [TableIgnore]
        public MultiPolygon? geometry { get; set; }
        public virtual Project Project { get; set; }

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