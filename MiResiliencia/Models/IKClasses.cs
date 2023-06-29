using MiResiliencia.Resources.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class IKClasses : IComparable
    {
        public int ID { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
        public virtual List<PrA> PrAs { get; set; }
        public virtual List<Intensity> Intensities { get; set; }

        public int CompareTo(object obj)
        {
            return Description.CompareTo(((IKClasses)obj).Description);
        }

        public override string ToString()
        {
            return $"{Value} {ResModel.IK_Years}";
        }
    }
}