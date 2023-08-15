using Microsoft.Extensions.Hosting;
using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;
using System;

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


        /// <summary>
        /// Net present value
        /// </summary>
        [TableIgnore]        
        public double NPV
        {
            get
            {
                try
                {
                    double npv = CalculateNPV(CashFlows, DiscountRate);
                    return npv;
                }
                catch (Exception ex)
                {
                    return -999999;
                }
            }
        }

        /// <summary>
        /// Internal Rate of Return in Percent [%]
        /// </summary>
        [TableIgnore]
        public double IRR
        {
            get
            {
                try
                {
                    double irr = CalculateIRR(CashFlows) * 100.0d;              //bisection method
                    //double irr = CalculateIRR_Newton(CashFlows) * 100.0d;     //using more robust calculation
                    return irr;
                }
                catch (Exception ex)
                {
                    return -999999;
                }
            }
        }

        /// <summary>
        /// dicount rate for npv calculation
        /// </summary>
        [TableIgnore]
        public double DiscountRate
        {
            get
            {
                double discountRate = ProtectionMeasure.DiscountRate / 100.0d;
                return discountRate;
            }
        }

        /// <summary>
        /// dicount rate for npv calculation in Percent [%]
        /// </summary>
        [TableIgnore]
        public double DiscountRatePercent
        {
            get
            {
                return DiscountRate * 100.0d;
            }
        }

        [TableIgnore]
        public double[] CashFlows           
        {
            get
            {
                double yearlyBenefit = (CollectiveRiskTotalBefore - CollectiveRiskTotalAfter)   //risk reduction
                                        - ProtectionMeasure.OperatingCosts
                                        - ProtectionMeasure.MaintenanceCosts;

                double[] startCosts = { -ProtectionMeasure?.Costs ?? 0 };
                double[] cashFlows = startCosts.Concat(Enumerable.Repeat(yearlyBenefit, ProtectionMeasure?.LifeSpan ?? 0).ToArray()).ToArray();

                return cashFlows;
            }
        }

        [TableIgnore]
        public string LogNPV
        {
            get
            {
                if (ProtectionMeasure == null)
                {
                    return $"ERROR: Protection Measure not defined";
                }
                string _result = "NPV = ";
                string _result2 = "NPV = ";

                var cashFlows = CashFlows;
                var discountRate = DiscountRate;
                for (int i = 0; i < cashFlows.Length; i++)
                {
                    if (i > 0)
                    {
                        _result += "+";
                        _result2 += "+";
                    }
                    _result += $" cashFlows[{i}] / (1 + discountRate)^{i} ";
                    _result2 += $" {cashFlows[i]:C0} / (1 + {discountRate})^{i} ";
                }

                _result += "; \n" + _result2;

                return _result;
            }
        }

        [TableIgnore]
        public string LogCashFlows
        {
            get
            {
                if (ProtectionMeasure == null)
                {
                    return $"ERROR: Protection Measure not defined";
                }

                string _result =
                    $"startCosts = -ProtectionMeasure.Costs ; \n" +
                    $"lifeSpan = ProtectionMeasure.LifeSpan; \n" +
                    $"yearlyBenefit = (CollectiveRiskTotalBefore - CollectiveRiskTotalAfter) " +
                    $" - ProtectionMeasure.OperatingCosts " +
                    $" - ProtectionMeasure.MaintenanceCosts; ";

                _result +=
                    $"startCosts = {-ProtectionMeasure?.Costs:C0}; \n" +
                    $"lifeSpan = {ProtectionMeasure?.LifeSpan}; \n" +
                    $"yearlyBenefit = ({CollectiveRiskTotalBefore:C0} - {CollectiveRiskTotalAfter:C0}) " +
                    $"- {ProtectionMeasure?.OperatingCosts:C0} " +
                    $"- {ProtectionMeasure?.MaintenanceCosts:C0}; ";

                return _result;
            }
        }

        [TableIgnore]
        public NpvIrrViewModel NpvIrrVM
        {
            get
            {
                return new NpvIrrViewModel()
                {
                    DiscountRatePercent = DiscountRatePercent,
                    IRRPercent = IRR,
                    NPV = NPV,
                    LogCashflow = LogCashFlows + "\nArray = " + string.Join(", ", CashFlows.Select(c => c.ToString("C0"))),
                    LogNPV = LogNPV
                };
            }
        }

        static double CalculateNPV(double[] cashFlows, double discountRate)
        {
            double npv = 0;
            for (int i = 0; i < cashFlows.Length; i++)
            {
                npv += cashFlows[i] / Math.Pow(1 + discountRate, i);
            }
            return npv;
        }

        /// <summary>
        /// bisection method to approximate the IRR
        /// </summary>
        /// <param name="cashFlows"></param>
        /// <returns></returns>
        static double CalculateIRR(double[] cashFlows)
        {
            double lowerRate = -1.0;
            double upperRate = 1.0;
            double epsilon = 0.00001;

            while (upperRate - lowerRate > epsilon)
            {
                double guessRate = (lowerRate + upperRate) / 2;
                double npv = CalculateNPV(cashFlows, guessRate);

                if (npv > 0)
                {
                    lowerRate = guessRate;
                }
                else
                {
                    upperRate = guessRate;
                }
            }

            return (lowerRate + upperRate) / 2;
        }

        /// <summary>
        /// function uses the Newton-Raphson method to iteratively approximate the IRR
        /// </summary>
        /// <param name="cashFlows"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        static double CalculateIRR_Newton(double[] cashFlows)
        {
            const double epsilon = 1.0e-6;
            const double maxIterations = 1000;

            double irr = 0.1; // Initial guess for IRR
            double previousIrr = 0;

            int iterations = 0;
            while (Math.Abs(irr - previousIrr) > epsilon && iterations < maxIterations)
            {
                previousIrr = irr;
                irr = previousIrr - CalculateNPV(cashFlows, previousIrr) / CalculateDerivative(cashFlows, previousIrr);
                iterations++;
            }

            if (iterations == maxIterations)
            {
                throw new InvalidOperationException("IRR calculation did not converge within the maximum number of iterations.");
            }

            return irr;
        }

        static double CalculateDerivative(double[] cashFlows, double rate)
        {
            double derivative = 0;
            for (int i = 0; i < cashFlows.Length; i++)
            {
                derivative += -i * cashFlows[i] / Math.Pow(1 + rate, i + 1);
            }
            return derivative;
        }

    }

    /// <summary>
    ///  View Model for the results of the NPV/IRR module (VAN/TIR)
    /// </summary>
    public class NpvIrrViewModel
    {

        public NpvIrrViewModel()
        {
        }

        [LocalizedDisplayName(nameof(ResSummary.TXT_DiscountRate), typeof(ResSummary))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_PercentF3), typeof(ResFormat))]
        public double DiscountRatePercent { get; set; }

        [LocalizedDisplayName(nameof(ResSummary.TXT_NPV), typeof(ResSummary))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public double NPV { get; set; }

        [LocalizedDisplayName(nameof(ResSummary.TXT_IRR), typeof(ResSummary))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_PercentF3), typeof(ResFormat))]
        public double IRRPercent { get; set; }

        [ShowInDetail]
        public string LogNPV { get; set; }

        //[ShowInDetail]
        //public string LogIRR { get; set; }

        [ShowInDetail]
        public string LogCashflow { get; set; }

    }
}