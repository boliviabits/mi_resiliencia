using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class MappedObject
    {
        public int ID { get; set; }
        public virtual Objectparameter Objectparameter { get; set; }
        public virtual Objectparameter? FreeFillParameter { get; set; }

        public virtual List<ResilienceValues> ResilienceValues { get; set; }
        public virtual List<DamageExtent> DamageExtents { get; set; }
        public virtual Project Project { get; set; }

        [NotMapped]
        public double lat { get; set; }
        [NotMapped]
        public double lon { get; set; }

        public Geometry geometry { get; set; }


        public static string ParseLatitude(double value)
        {
            var direction = value < 0 ? 1 : 0;
            return MappedObject.ParseLatituteOrLongitude(value, direction);
        }

        public static string ParseLongitude(double value)
        {
            var direction = value < 0 ? 1 : 0;
            return MappedObject.ParseLatituteOrLongitude(value, direction);
        }

        //This must be a private method because it requires the caller to ensure
        //that the direction parameter is correct.
        public static string ParseLatituteOrLongitude(double value, int direction)
        {
            value = Math.Abs(value);

            var degrees = Math.Truncate(value);

            value = (value - degrees) * 60;

            var minutes = Math.Truncate(value);
            var seconds = (value - minutes) * 60;

            return degrees + "° " + minutes + "' " + Math.Round(seconds) + "\"";
        }


        //----------------------------------------------------------
        //not in DB
        [NotMapped]
        public virtual bool IsClipped { get; set; } = false;
        [NotMapped]
        public virtual Intensity Intensity { get; set; } = null;
        //public virtual double ResilienceFactor => DamagePotentialController.computeResilienceFactor(ResilienceValues?.ToList());

        public override string ToString()
        {
            return $"{ID} - " +
                $"{Objectparameter?.ObjectClass?.Name ?? "ERROR"} - " +
                $"{Objectparameter?.Name ?? "ERROR"} " +
                $"({geometry?.GeometryType ?? "ERROR"})";
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}