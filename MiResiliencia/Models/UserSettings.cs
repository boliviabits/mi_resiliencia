using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiResiliencia.Models
{
    public enum CoordinateSystem
    {
        SwissGrid = 21708,
        WGS84 = 4326
    }

    public class UserSettings
    {

        public int Id { get; set; }

        public bool LabelsEnabled { get; set; }

        public string? BaseMapSource { get; set; }

        [DefaultValue(CoordinateSystem.SwissGrid)]
        public CoordinateSystem CoordinateSystem { get; set; }

        public virtual List<LayersEnabledByUser> EnabledLayers { get; set; }

        public string? Language { get; set; }

        public int DrawWindowX { get; set; }
        public int DrawWindowY { get; set; }
        public int DrawWindowWidth { get; set; }
        public int DrawWindowHeight { get; set; }

        public int LayerWindowX { get; set; }
        public int LayerWindowY { get; set; }
        public int LayerWindowWidth { get; set; }
        public int LayerWindowHeight { get; set; }

        public int ShpWindowX { get; set; }
        public int ShpWindowY { get; set; }
        public int ShpWindowWidth { get; set; }
        public int ShpWindowHeight { get; set; }
    }
}