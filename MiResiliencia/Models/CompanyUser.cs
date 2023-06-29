using System.ComponentModel.DataAnnotations.Schema;

namespace MiResiliencia.Models
{
    [Table("CompanyUsers")]
    public class CompanyUser
    {
        public string CompanyUserRefId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int UserRefId { get; set; }
        public virtual Company Company { get; set; }
    }
}
