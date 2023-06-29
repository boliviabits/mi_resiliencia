using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class Project : IComparable
    {
        public int Id { get; set; }
        [DisplayName("Nombre")]
        [Required]
        public string? Name { get; set; }
        [DisplayName("Número")]
        [Required]
        public string? Number { get; set; }
        [Display(ResourceType = typeof(Resources.Global), Name = "Description")]
        public string? Description { get; set; }

        [DefaultValue(CoordinateSystem.WGS84)]
        public CoordinateSystem CoordinateSystem { get; set; }

        public virtual Company Company { get; set; }

        public virtual List<Intensity> Intesities { get; set; }
        public virtual List<MappedObject> MappedObjects { get; set; }
        public virtual List<PrA> PrAs { get; set; }

        public virtual ProtectionMeasure ProtectionMeasure { get; set; }

        public virtual ProjectState ProjectState { get; set; }
        public Polygon? geometry { get; set; }

        public int CompareTo(object obj)
        {
            if (typeof(Project) == obj.GetType())
            {
                return Name.CompareTo(((Project)obj).Name);
            }
            else return 1;
        }
        public override string ToString()
        {
            return $"Project {Id} - {Name}";
        }
    }
}