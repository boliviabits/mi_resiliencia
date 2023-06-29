using System.ComponentModel.DataAnnotations.Schema;

namespace MiResiliencia.Models
{
    public class CompanyAdmin
    {
        public string AdminRefId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int UserRefId { get; set; }
        public virtual Company Company { get; set; }
    }
}
