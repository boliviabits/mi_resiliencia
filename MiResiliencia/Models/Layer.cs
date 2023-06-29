using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class Layer
    {
        public int ID { get; set; }
        [DisplayName("Titel")]
        public string Title { get; set; }
        [DisplayName("Javascript ID")]
        public string JavascriptID { get; set; }
        [DisplayName("URL")]
        public string URL { get; set; }
        [DisplayName("OL Layer Code")]
        public string OLCode { get; set; }
        [DisplayName("Legende Bild")]
        public int? LegendID { get; set; }
        [DisplayName("Reihenfolge")]
        public int Sort { get; set; }

        public virtual File Legend { get; set; }

        public int CompanyID { get; set; }
        public virtual Company Company { get; set; }

        public virtual List<LayersEnabledByUser> EnabledByUser { get; set; }

        [NotMapped]
        public bool Enabled { get; set; }
    }
}