using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;

namespace MiResiliencia.Models.API
{
    public class DamageExtentError
    {
        [LocalizedDisplayName(nameof(ResModel.DE_MappedObject), typeof(ResModel))]
        public MappedObject MappedObject { get; set; }
        [LocalizedDisplayName(nameof(ResModel.DE_Intensity), typeof(ResModel))]
        public Intensity Intensity { get; set; }
        [LocalizedDisplayName(nameof(ResModel.DE_Issue), typeof(ResModel))]
        public string Issue { get; set; }
    }
}