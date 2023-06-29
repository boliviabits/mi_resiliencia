using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MiResiliencia.Models
{
    public enum IntensityDegree {
        [Display(ResourceType = typeof(Resources.Global), Name = "strong")]
        alta = 0,
        [Display(ResourceType = typeof(Resources.Global), Name = "mittel")]
        media = 1,
        [Display(ResourceType = typeof(Resources.Global), Name = "weak")]
        baja = 2
    }
    public class Intensity : ICloneable
    {
        public int ID { get; set; }
        public virtual Project Project { get; set; }
        public int NatHazardID { get; set; }
        public virtual NatHazard NatHazard { get; set; }
        public int IKClassesID { get; set; }
        public virtual IKClasses IKClasses { get; set; }
        public bool BeforeAction { get; set; }
        public IntensityDegree IntensityDegree { get; set; }
        public virtual List<DamageExtent> DamageExtents { get; set; }
        public MultiPolygon geometry { get; set; }


        Dictionary<int, string> IntensityDegreeDic = new Dictionary<int, string>() {
            { 0, "high" },
            { 1, "medium" },
            { 2, "low" } };

        public override string ToString()
        {
            return $"{ID} - {NatHazard?.Name} " +
                   $"{IKClasses?.Description}, {IntensityDegreeDic[(int)IntensityDegree]}, " +
                   $"before={BeforeAction}";
            //$"geometryExists={geometry != null}";
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}