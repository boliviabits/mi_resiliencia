using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public enum StrokeOrFill
    {
        Stroke, Fill
    }
    public class LayerStyle
    {
        public int ID { get; set; }
        public StrokeOrFill StrokeOrFill { get; set; }
        public string Layer { get; set; }
        public string Attribute { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }
        public string? Function { get; set; }
        
    }
}