using MiResiliencia.Helpers.API;
using MiResiliencia.Resources.API;
using NetTopologySuite.Geometries;

namespace MiResiliencia.Models
{
    public class DamageExtent
    {
        [TableIgnore]
        public int MappedObjectId { get; set; }
        [TableIgnore]
        public int IntensityId { get; set; }

        [LocalizedDisplayName(nameof(ResModel.DE_MappedObject), typeof(ResModel))]
        public virtual MappedObject MappedObject { get; set; }  // FK \__
        [LocalizedDisplayName(nameof(ResModel.DE_Intensity), typeof(ResModel))]
        public virtual Intensity Intensity { get; set; }        // FK /  \>PK 

        [TableIgnore]
        public virtual Geometry geometry { get; set; }

        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Area), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Meter2_F3), typeof(ResFormat))]
        public double Area { get; set; }    //area [m^2]
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Length), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Meter_F3), typeof(ResFormat))]
        public double Length { get; set; }  //length [m^1]
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Piece), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_F0), typeof(ResFormat))]
        public double Piece { get; set; }  //count [1]
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Clipped), typeof(ResModel))]
        public bool Clipped { get; set; } //flag, indicating that geometry has been clipped
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Part), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_F3), typeof(ResFormat))]
        public virtual double Part { get; set; }

        [LocalizedDisplayName(nameof(ResModel.DE_PersonDamage), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public double PersonDamage { get; set; }
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_LogPersonDamage), typeof(ResModel))]
        public string? LogPersonDamage { get; set; }

        [LocalizedDisplayName(nameof(ResModel.DE_PropertyDamage), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public double PropertyDamage { get; set; }
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_LogPropertyDamage), typeof(ResModel))]
        public string? LogPropertyDamage { get; set; }

        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Deaths), typeof(ResModel))]
        public double Deaths { get; set; }
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_LogDeaths), typeof(ResModel))]
        public string? LogDeaths { get; set; }

        [TableIgnore]
        [LocalizedDisplayName(nameof(ResModel.DE_DeathProbability), typeof(ResModel))]
        public double DeathProbability { get; set; }
        [TableIgnore]
        [LocalizedDisplayName(nameof(ResModel.DE_LogDeathProbability), typeof(ResModel))]
        public string? LogDeathProbability { get; set; }

        [LocalizedDisplayName(nameof(ResModel.DE_IndirectDamage), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public double IndirectDamage { get; set; }       // before resilience
        public string? LogIndirectDamage { get; set; }       // before resilience

        [LocalizedDisplayName(nameof(ResModel.DE_ResilienceFactor), typeof(ResModel))]
        public double ResilienceFactor { get; set; }     // 0, if no resilience
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_LogIndirectDamage), typeof(ResModel))]
        public string? LogResilienceFactor { get; set; }     // 0, if no resilience

        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_Log), typeof(ResModel))]
        public string? Log { get; set; }


        //*not in db*
        [LocalizedDisplayName(nameof(ResModel.DE_ResilientIndirectDamage), typeof(ResModel))]
        [LocalizedDisplayFormat(nameof(ResFormat.DF_Currency), typeof(ResFormat))]
        public virtual double ResilientIndirectDamage
        {
            get
            {
                return IndirectDamage * (1.0d - ResilienceFactor);
            }
        }
        [ShowInDetail]
        [LocalizedDisplayName(nameof(ResModel.DE_LogResilientIndirectDamage), typeof(ResModel))]
        public virtual string LogResilientIndirectDamage
        {
            get
            {
                return $"ResilientIndirectDamage = IndirectDamage * (1 - ResilienceFactor); \n" +
                       $"ResilientIndirectDamage = {IndirectDamage:C} * (1 - {ResilienceFactor:F3})";
            }
        }
        //end of *not in db*

        public override bool Equals(object obj)
        {
            var other = obj as DamageExtent;

            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.MappedObject.ID == other.MappedObject.ID &&
                this.Intensity.ID == other.Intensity.ID;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 31) ^ MappedObject.GetHashCode();
                hash = (hash * 31) ^ Intensity.GetHashCode();

                return hash;
            }
        }
    }

}