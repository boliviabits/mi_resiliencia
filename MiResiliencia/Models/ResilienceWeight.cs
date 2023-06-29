using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ResilienceWeight
    {
        public int ID { get; set; }

        public virtual ResilienceFactor ResilienceFactor { get; set; }

        public virtual NatHazard NatHazard { get; set; }
        public double Weight { get; set; }

        public bool? BeforeAction { get; set; }

        public virtual List<ResilienceValues> ResilienceValues { get; set; }


    }
}