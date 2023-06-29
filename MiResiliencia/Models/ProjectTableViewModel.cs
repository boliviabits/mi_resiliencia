using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ProjectTableViewModel
    {
        public Project Project { get; set; }
        public bool CanUserView { get; set; }
        public bool CanUserEdit { get; set; }
    }
}