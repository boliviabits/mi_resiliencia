using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ObjectparameterHasProperties
    {
        public int ID { get; set; }
        public virtual Objectparameter Objectparameter { get; set; }
        public string Property { get; set; }
        public bool isOptional { get; set; }
    }
}