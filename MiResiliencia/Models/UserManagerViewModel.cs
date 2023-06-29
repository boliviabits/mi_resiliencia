using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class UserManagerViewModel
    {
        public Controllers.Rights Rights { get; set; }
        public SelectList Companies { get; set; }

        public RegisterViewModel NewUser { get; set; }

    }
}