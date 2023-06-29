using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class PostGISHatObjektparameter
    {
        public int ID { get; set; }
        public int PostGISID { get; set; }
        public virtual Objectparameter Objektparameter { get; set; }
    }
}