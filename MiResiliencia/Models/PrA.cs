using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public class PrA
    {
        [LocalizedDisplayName(nameof(ResModel.PA_Hazard), typeof(ResModel))]
        public virtual NatHazard NatHazard { get; set; }
        [Key, Column(Order = 0)]
        [TableIgnore]
        public int NatHazardId { get; set; }
        
        [Key, Column(Order = 1)]
        [TableIgnore]
        public int IKClassesId { get; set; }
        [LocalizedDisplayName(nameof(ResModel.PA_ReturnPeriod), typeof(ResModel))]
        public virtual IKClasses IKClasses { get; set; }
        
        [Key, Column(Order = 2)]
        [TableIgnore]
        public int ProjectId { get; set; }
        [TableIgnore]
        public virtual Project Project { get; set; }

        [LocalizedDisplayName(nameof(ResModel.PA_Value), typeof(ResModel))]
        public double Value { get; set; }

    }
}