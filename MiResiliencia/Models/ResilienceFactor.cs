using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ResilienceFactor
    {
        public string ID { get; set; }
        public string Preparedness { get; set; }

        public virtual List<ResilienceWeight> ResilienceWeights { get; set; }

        public virtual List<ObjectparameterHasResilienceFactor> ObjectparameterHasResilienceFactor { get; set; }
    }
    
}