using System.ComponentModel.DataAnnotations.Schema;

namespace MiResiliencia.Models
{
    [Table("ObjectparameterHasResilienceFactors")]
    public class ObjectparameterHasResilienceFactor
    {
        public string ResilienceFactor_ID { get; set; }
        public virtual ResilienceFactor ResilienceFactor { get; set; }
        public int Objectparameter_ID { get; set; }
        public virtual Objectparameter Objectparameter { get; set; }
    }
}
