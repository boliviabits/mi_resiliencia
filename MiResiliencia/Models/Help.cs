using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MiResiliencia.Models
{
    public class Help
    {
        public int ID { get; set; }
        public string Wikiname { get; set; }
        public string Language { get; set; }
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

    }
}