using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class NatHazard : IComparable
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public virtual List<PrA> PrAs { get; set; }
        public virtual List<Intensity> Intensities { get; set; }
        public virtual List<ObjectparameterPerProcess> ObjectparameterPerProcesses { get; set; }

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((NatHazard)obj).Name);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ID.Equals(((NatHazard)obj).ID);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}