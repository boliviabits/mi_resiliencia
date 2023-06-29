using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MiResiliencia.Models
{

    public class Objectparameter : ICloneable
    {
        public int ID { get; set; }
        public virtual ObjectClass? ObjectClass { get; set; }

        public FeatureType FeatureType { get; set; }

        [Display(ResourceType = typeof(Resources.Global), Name = "ObjectName")]
        public string? Name { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Description")]
        public string? Description { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "ValuePerUnity")]
        public int Value { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "MotivationToChnageValue")]
        public string? ChangeValue { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "UnityOfObject")]
        public string? Unity { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Floors")]
        public int Floors { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "PersonPerFloor")]
        public int Personcount { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "MotivationToChnageValue")]
        public string? ChangePersonCount { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "PersonPresence")]
        public double Presence { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "CarsPerDay")]
        public int NumberOfVehicles { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Velocity")]
        public int Velocity { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Employees")]
        public int Staff { get; set; }


        [DisplayName("")]
        public bool IsStandard { get; set; }

        public virtual List<ObjectparameterPerProcess> ObjectparameterPerProcesses { get; set; }

        public virtual Objectparameter? MotherOtbjectparameter { get; set; }

        public virtual List<ObjectparameterHasProperties> HasProperties { get; set; }

        public virtual List<ObjectparameterHasResilienceFactor> ResilienceFactors { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public override string ToString()
        {
            return $"{ID} - {Name ?? "no name"} - Class {ObjectClass?.Name ?? "no class"}";
        }
    }
}