using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ObjectparameterViewModel
    {
        public MappedObject MappedObject { get; set; }
        public Objectparameter MergedObjectparameter { get; set; }
        public List<ObjectparameterHasProperties> HasProperties { get; set; }

    }

    public class MultipleObjectparameterViewModel
    {
        public List<MappedObject> MappedObjects { get; set; }
        public Objectparameter MergedObjectparameter { get; set; }
        public List<ObjectparameterHasProperties> HasProperties { get; set; }

    }
}