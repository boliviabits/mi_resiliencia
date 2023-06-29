using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;

namespace MiResiliencia.Models.API
{
    public class ProjectResult
    {
        [TableIgnore]
        public bool ShowDetails { get; set; } = false;

        [TableIgnore]
        //[DisplayName("Creation Time of Project Result")]
        [LocalizedDisplayName(nameof(ResResult.PR_CreationTime), typeof(ResResult))]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// NatHazards in this project
        /// </summary>
        [TableIgnore]
        public List<NatHazard> NatHazards { get; set; } = new List<NatHazard>();

        [TableIgnore]
        public List<DamageExtentError> DamageExtentErrors { get; set; }

        [TableIgnore]
        public Project Project { get; set; }

        [TableIgnore]
        public ProtectionMeasure ProtectionMeasure { get; set; }

        [TableIgnore]
        public string Message { get; set; }

        [TableIgnore]
        public List<ProcessResult> ProcessResults { get; set; } = new List<ProcessResult>();

        [TableIgnore]
        public List<ProcessResult> ProcessResultsSorted => ProcessResults.OrderByDescending(p => p.BeforeAction).ThenBy(p => p.NatHazard.ID).ToList();

        //[DisplayName("Collective Risk Total - Before Measure")]
        [LocalizedDisplayName(nameof(ResResult.PR_CollectiveRiskTotalBefore), typeof(ResResult))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public double CollectiveRiskTotalBefore =>
            ProcessResults.Where(p => p.BeforeAction == true).Sum(p => p.CollectiveRiskTotal.Item1);

        //[DisplayName("Collective Risk Total - After Measure")]
        [LocalizedDisplayName(nameof(ResResult.PR_CollectiveRiskTotalAfter), typeof(ResResult))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public double CollectiveRiskTotalAfter =>
            ProcessResults.Where(p => p.BeforeAction == false).Sum(p => p.CollectiveRiskTotal.Item1);

        //[DisplayName("Collective Risk Total - Reduction")]
        [LocalizedDisplayName(nameof(ResResult.PR_RiskReduction), typeof(ResResult))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public double RiskReduction
        {
            get
            {
                if (CollectiveRiskTotalAfter > CollectiveRiskTotalBefore)
                    return 0.0;
                else
                    return CollectiveRiskTotalBefore - CollectiveRiskTotalAfter;
            }
        }

        //[DisplayName("Protection Measure - Yearly Costs")]
        [LocalizedDisplayName(nameof(ResResult.PR_ProtectionMeasureYearlyCosts), typeof(ResResult))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_CurrencyPerYear), typeof(ResFormat))]
        public double ProtectionMeasureYearlyCosts => ProtectionMeasure?.YearlyCosts ?? 0.0d;

        //[DisplayName("Benefit / Cost - Ratio")]
        [LocalizedDisplayName(nameof(ResResult.PR_BenefitCostRatio), typeof(ResResult))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_F1), typeof(ResFormat))]
        public double BenefitCostRatio
        {
            get
            {
                if (ProtectionMeasureYearlyCosts == 0)
                    //if (RiskReduction == 0)
                    return 0.0d;
                //else
                //return double.MaxValue;
                else
                    return Math.Round(RiskReduction / ProtectionMeasureYearlyCosts, 3);
            }
        }

        public ProjectResult()
        {
            CreationDate = DateTime.Now;
        }

    }
}