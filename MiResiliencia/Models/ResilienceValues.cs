using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ResilienceValues
    {
        public int ID { get; set; }
        public virtual MappedObject MappedObject { get; set; }
        public virtual ResilienceWeight ResilienceWeight { get; set; }
        public double OverwrittenWeight { get; set; }
        public double Value { get; set; }

        //not in db
        public virtual double Weight
        {
            get
            {
                if (OverwrittenWeight >= 0)
                {
                    return OverwrittenWeight;
                }
                else
                {
                    return ResilienceWeight?.Weight ?? 0.0;
                }
            }
        }

        public override string ToString()
        {

            return $"Value: {Value} / Weight: {Weight}";
        }
    }
}