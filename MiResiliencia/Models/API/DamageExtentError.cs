namespace MiResiliencia.Models.API
{
    public class DamageExtentError
    {
        public MappedObject MappedObject { get; set; }
        public Intensity Intensity { get; set; }
        public string Issue { get; set; }
    }
}