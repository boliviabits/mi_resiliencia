namespace MiResiliencia.Models
{
    public class LayersEnabledByUser
    {
        public int UserSettingsId { get; set; }
        public virtual UserSettings UserSettings { get; set; }
        public int LayersId { get; set; }
        public virtual Layer Layer { get; set; }
    }
}
