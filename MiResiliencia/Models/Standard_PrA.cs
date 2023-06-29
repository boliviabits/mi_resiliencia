using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class Standard_PrA
    {
        [Key, Column(Order = 0)]
        public int NatHazardId { get; set; }
        [Key, Column(Order = 1)]
        public int IKClassesId { get; set; }
        public virtual NatHazard NatHazard { get; set; }
        public virtual IKClasses IKClasses { get; set; }
        public double Value { get; set; }
    }
}