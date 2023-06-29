using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public enum FeatureType
    {
        Point = 0, 
        Line = 1, 
        Polygon = 2
    }
    public class ObjectClass
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
    }
}