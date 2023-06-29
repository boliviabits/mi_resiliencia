using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;

namespace MiResiliencia.Models
{
    public class ObjectparameterPerProcess
    {
        public int ID { get; set; }
        public virtual NatHazard NatHazard { get; set; }
        public virtual Objectparameter Objektparameter { get; set; }

        // Vulnerability por intensidad
        [Display(ResourceType = typeof(Resources.Global), Name = "strong")]
        public double VulnerabilityLow { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "mittel")]
        public double VulnerabilityMedium { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "weak")]
        public double VulnerabilityHigh { get; set; }

        // Mortalidad por intensidad
        [Display(ResourceType = typeof(Resources.Global), Name = "strong")]
        public double MortalityLow { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "mittel")]
        public double MortalityMedium { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "weak")]
        public double MortalityHigh { get; set; }

        // Dano indirecto
        public double Value { get; set; }
        public string Unit { get; set; }

        //Vulnerability of indirect damage
        [Display(ResourceType = typeof(Resources.Global), Name = "strong")]
        public double IndirectVulnerabilityLow { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "mittel")]
        public double IndirectVulnerabilityMedium { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "weak")]
        public double IndirectVulnerabilityHigh { get; set; }

        // Duracion de dano indirecto
        [Display(ResourceType = typeof(Resources.Global), Name = "strong")]
        public double DurationLow { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "mittel")]
        public double DurationMedium { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "weak")]
        public double DurationHigh { get; set; }

    }
}