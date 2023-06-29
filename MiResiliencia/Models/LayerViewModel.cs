using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class LayerViewModel
    {
        public bool LabelsEnabled { get; set; }

        public string BaseMapSource { get; set; }

        public CoordinateSystem CoordinateSystem { get; set; }
        public virtual List<Layer> Layers { get; set; }

    }
}