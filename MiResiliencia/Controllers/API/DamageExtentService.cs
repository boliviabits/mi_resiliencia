using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MiResiliencia.Helpers;
using MiResiliencia.Helpers.API;
using MiResiliencia.Models;
using MiResiliencia.Models.API;
using MiResiliencia.Resources.API;
using NetTopologySuite.Geometries;
using NuGet.ProjectModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace MiResiliencia.Controllers.API
{
    public class DamageExtentService
    {
        private readonly MiResilienciaContext _context;
        private readonly ILogger<DamageExtentService> _logger;
        //private readonly IntensityService _intensityService;
        private readonly ThreadSafeFileWriter _safeFileWriter;

        public DamageExtentService(
            MiResilienciaContext context,
            ILogger<DamageExtentService> logger
            //IntensityService intensityService
            )
        {
            _context = context;
            _logger = logger;
            _safeFileWriter = new ThreadSafeFileWriter();
            //_intensityService = intensityService;
        }

        private double _willingnessToPay = -1;
        public double WillingnessToPay
        {
            get
            {
                if (_willingnessToPay == -1)
                {
                    WillingnessToPay willingnessToPay = _context.WillingnessToPays.Find(1);
                    _willingnessToPay = willingnessToPay.Value;
                }
                return _willingnessToPay;
            }
        }

        /// <summary>
        /// Get the merged Objectparameter for a MappedObject
        /// </summary>
        /// <param name="mapObj"></param>
        /// <returns></returns>
        public async Task<Objectparameter> GetMergedObjectParameter(MappedObject mapObjDettached)
        {
            //Reload for lazyloading
            var mapObj = await _context.MappedObjects
                .Include(m => m.Objectparameter.ObjectparameterPerProcesses)
                .Include(m => m.Objectparameter.HasProperties)
                .Include(m => m.Objectparameter.ObjectClass)
                .Include(m => m.Objectparameter.ResilienceFactors)
                .Include(m => m.Objectparameter.MotherOtbjectparameter.ObjectparameterPerProcesses)
                .Include(m => m.Objectparameter.MotherOtbjectparameter.HasProperties)
                .Include(m => m.Objectparameter.MotherOtbjectparameter.ObjectClass)
                .Include(m => m.Objectparameter.MotherOtbjectparameter.ResilienceFactors)
                .Include(m => m.ResilienceValues)
                .Where(m => m.ID == mapObjDettached.ID)
                .SingleOrDefaultAsync();

            if (mapObj == null)
                throw new NullReferenceException(mapObj.ToString());

            // default take everything from original
            Objectparameter mergedObjParam = (Objectparameter)mapObj.Objectparameter.Clone();

            // look for free fill properties
            if (mapObj.Objectparameter.MotherOtbjectparameter != null)
            {
                mergedObjParam.HasProperties = mapObj.Objectparameter.MotherOtbjectparameter.HasProperties;
                mergedObjParam.ObjectparameterPerProcesses = mapObj.Objectparameter.MotherOtbjectparameter.ObjectparameterPerProcesses;
                mergedObjParam.FeatureType = mapObj.Objectparameter.MotherOtbjectparameter.FeatureType;
                mergedObjParam.ObjectClass = mapObj.Objectparameter.MotherOtbjectparameter.ObjectClass;
            }
            else
            {
                mergedObjParam.HasProperties = mapObj.Objectparameter.HasProperties;
                mergedObjParam.FeatureType = mapObj.Objectparameter.FeatureType;
                mergedObjParam.ObjectClass = mapObj.Objectparameter.ObjectClass;
            }

            // copy all free fill properties to merged parameter, wenn free fill has a value
            foreach (ObjectparameterHasProperties ohp in mergedObjParam.HasProperties.Where(m => m.isOptional == true))
            {
                if (mapObj.FreeFillParameter != null)
                {
                    // Get the Property from the Free Fill Object
                    PropertyInfo FreeFillProperty = ohp.Property?.Split('.').Select(s => mapObj.FreeFillParameter.GetType().GetProperty(s)).FirstOrDefault();
                    // Get the Property from the Merged Object
                    PropertyInfo MergedProperty = ohp.Property?.Split('.').Select(s => mergedObjParam.GetType().GetProperty(s)).FirstOrDefault();

                    // Assign FreeFill PropertyValue to Merged PropertyValue
                    if (FreeFillProperty.GetValue(mapObj.FreeFillParameter) != null)
                    {
                        MergedProperty.SetValue(mergedObjParam, FreeFillProperty.GetValue(mapObj.FreeFillParameter));
                    }
                }
            }

            return mergedObjParam;
        }

        /// <summary>
        /// Compuation of all Damage Extent of a Damage Potential in a given Intensity
        /// </summary>
        /// <param name="mapObj">Damage Potential</param>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public async Task<DamageExtent> ComputeDamageExtent(MappedObject mapObj, Intensity intensity)//, List<MappedObject> clippedObjects = null)
        {
            var _damageExtent = new DamageExtent()
            {
                Intensity = (Intensity)intensity.Clone(),        //make sure shallow copy is used
                MappedObject = (MappedObject)mapObj.Clone(),     //make sure shallow copy is used
                Clipped = mapObj.IsClipped,
                geometry = mapObj.geometry,
            };

            int _intensityDegree = (int)intensity.IntensityDegree; //0=high, 1=med, 2=low

            //Merge Objectparameter with Freefillparamter
            var _mergedObjParam = await this.GetMergedObjectParameter(mapObj);

            //get Objectparameters for NatHazard (Vulnerability, Mortality, indirect costs)
            int _motherObjectID = _mergedObjParam.MotherOtbjectparameter != null
                                    ? _mergedObjParam.MotherOtbjectparameter.ID
                                    : _mergedObjParam.ID;

            ObjectparameterPerProcess _objectParamProcess;
            _objectParamProcess = _mergedObjParam.ObjectparameterPerProcesses
                                     .Where(pp => pp.NatHazard.ID == intensity.NatHazard.ID &&
                                                  pp.Objektparameter.ID == _motherObjectID)
                                     .SingleOrDefault();

            if (_objectParamProcess == null)
            {
                _damageExtent.Log += $"ERROR: NO PROCESS PARAMETER, count: {_mergedObjParam.ObjectparameterPerProcesses.Count} \n";
                return _damageExtent;
            }

            //get pra for intensity
            PrA _prA = this.GetPrA(intensity);

            if (_prA == null)
            {
                _damageExtent.Log += $"ERROR: NO PrA VALUES FOUND FOR PROCESS {intensity.NatHazard.Name.ToUpper()} \n";
                return _damageExtent;
            }


            // BUILDINGS and SPECIAL BUILDINGS
            if (_mergedObjParam.ObjectClass.ID <= 2)
            {

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PERSON DAMAGE

                double _deaths = _prA.Value * _mergedObjParam.Personcount * _mergedObjParam.Presence / 24.0;
                string _logDeaths1 = $"Deaths = prA * PersonCount * Presence/24";
                string _logDeaths2 = $"Deaths = {_prA.Value:F3} * {_mergedObjParam.Personcount} * {_mergedObjParam.Presence:F1} / 24";

                double _deathProbability = _mergedObjParam.Personcount > 0 ? 1.0d / _mergedObjParam.Personcount : 0;
                string _logDProb1 = _mergedObjParam.Personcount > 0 ? $"IndividualDeathRisk = 1 / PersonCount" : "ERROR: 1 / PersonCount";
                string _logDProb2 = _mergedObjParam.Personcount > 0 ? $"IndividualDeathRisk =  1 / {_mergedObjParam.Personcount}" : $"ERROR: 1 / {_mergedObjParam.Personcount}";
                if (_mergedObjParam.Personcount < 1)
                    _damageExtent.Log = $"{ResModel.DE_PersonCount} = {_mergedObjParam.Personcount} \n";

                //switching on intensity degree 
                switch (_intensityDegree)
                {
                    case 0:
                        _deaths *= _objectParamProcess.MortalityHigh;
                        _logDeaths1 += $" * MortalityHigh";
                        _logDeaths2 += $" * {_objectParamProcess.MortalityHigh:F3}";
                        break;
                    case 1:
                        _deaths *= _objectParamProcess.MortalityMedium;
                        _logDeaths1 += $" * MortalityMedium";
                        _logDeaths2 += $" * {_objectParamProcess.MortalityMedium:F3}";
                        break;
                    case 2:
                        _deaths *= _objectParamProcess.MortalityLow;
                        _logDeaths1 += $" * MortalityLow";
                        _logDeaths2 += $" * {_objectParamProcess.MortalityLow:F3}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                }

                //looking for floors count, if available for this object type
                if (_mergedObjParam.HasProperties.Where(m => m.Property == nameof(_mergedObjParam.Floors)).Any())
                {
                    _deaths *= _mergedObjParam.Floors;
                    _logDeaths1 += $" * Floors";
                    _logDeaths2 += $" * {_mergedObjParam.Floors}";

                    if (_mergedObjParam.Floors > 0)
                    {
                        _deathProbability /= _mergedObjParam.Floors;
                        _logDProb1 += $" / Floors";
                        _logDProb2 += $" / {_mergedObjParam.Floors}";
                    }
                    else
                        _damageExtent.Log += $"{ResModel.DE_Floors} = {_mergedObjParam.Floors} \n";
                }

                _damageExtent.Deaths = _deaths;
                _damageExtent.LogDeaths = _logDeaths1 + ";\n" + _logDeaths2;

                _deathProbability *= _deaths;
                _logDProb1 += $" * Deaths";
                _logDProb2 += $" * {_deaths:F6}";
                _damageExtent.DeathProbability = _deathProbability;
                _damageExtent.LogDeathProbability = _logDProb1 + ";\n" + _logDProb2;

                _damageExtent.PersonDamage = _deaths * WillingnessToPay;
                _damageExtent.LogPersonDamage = $"PersonDamage = Deaths * WillingnessToPay;\n";
                _damageExtent.LogPersonDamage += $"PersonDamage = {_deaths:F6} * {WillingnessToPay:C}";


                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PROPERTY DAMAGE

                double _propertyDamage = _prA.Value * _mergedObjParam.Value;
                string _logPropertyDamage1 = $"PropertyDamage = prA * Value";
                string _logPropertyDamage2 = $"PropertyDamage = {_prA.Value:F3} * {_mergedObjParam.Value:C}";

                switch ((int)_mergedObjParam.FeatureType)
                {
                    case 0: //POINT BASED OBJECT(like communication tower)
                        _damageExtent.Piece = 1;
                        _logPropertyDamage1 += $" * Piece";
                        _logPropertyDamage2 += $" * 1";
                        if (_damageExtent.Clipped)
                        {
                            _damageExtent.Part = 1.0d;
                        }

                        break;

                    case 1: //LINE BASED OBJECT (like Aduccion)
                        _damageExtent.Length = mapObj.geometry.Length;
                        if (_damageExtent.Clipped)
                        {
                            var rawMapObject = await _context.MappedObjects.FindAsync(mapObj.ID);
                            _damageExtent.Part = mapObj.geometry.Length / rawMapObject.geometry.Length;
                        }

                        _propertyDamage *= mapObj.geometry.Length;
                        _logPropertyDamage1 += $" * Length";
                        _logPropertyDamage2 += $" * {mapObj.geometry.Length:F3}";
                        break;

                    case 2: //POLYGON BASED OBJECT
                        _damageExtent.Area = mapObj.geometry.Area;
                        if (_damageExtent.Clipped)
                        {
                            var rawMapObject = await _context.MappedObjects.FindAsync(mapObj.ID);
                            _damageExtent.Part = mapObj.geometry.Area / rawMapObject.geometry.Area;
                        }

                        _propertyDamage *= mapObj.geometry.Area;
                        _logPropertyDamage1 += $" * Area";
                        _logPropertyDamage2 += $" * {mapObj.geometry.Area:F3}";
                        break;

                    default:
                        _damageExtent.Log += $"ERROR: BUILDING, FEATURETYPE = {_mergedObjParam.ObjectClass.ID}, {_mergedObjParam.FeatureType} \n";
                        return _damageExtent;

                }

                //looking for floors count, if available for this object type
                if (_mergedObjParam.HasProperties.Where(m => m.Property == nameof(_mergedObjParam.Floors)).Any())
                {
                    _propertyDamage *= _mergedObjParam.Floors;
                    _logPropertyDamage1 += $" * Floors";
                    _logPropertyDamage2 += $" * {_mergedObjParam.Floors}";
                }

                switch (_intensityDegree)
                {
                    case 0:
                        _propertyDamage *= _objectParamProcess.VulnerabilityHigh;
                        _logPropertyDamage1 += $" * VulnerabilityHigh";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityHigh:F3}";

                        break;
                    case 1:
                        _propertyDamage *= _objectParamProcess.VulnerabilityMedium;
                        _logPropertyDamage1 += $" * VulnerabilityMedium";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityMedium:F3}";
                        break;
                    case 2:
                        _propertyDamage *= _objectParamProcess.VulnerabilityLow;
                        _logPropertyDamage1 += $" * VulnerabilityLow";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityLow:F3}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                }

                _damageExtent.PropertyDamage = _propertyDamage;
                _damageExtent.LogPropertyDamage = _logPropertyDamage1 + ";\n" + _logPropertyDamage2;

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            }
            // INFRASTRUCTURE
            else if (_mergedObjParam.ObjectClass.ID == 3)
            {

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PERSON DAMAGE

                // STREETS / BRIDGES
                // LINE OBJECT
                if ((int)_mergedObjParam.FeatureType == 1)
                {
                    if (_mergedObjParam.Personcount == 0)
                    {
                        _damageExtent.Deaths = 0;
                        _damageExtent.LogDeaths = "no person damage";

                        _damageExtent.DeathProbability = 0;
                        _damageExtent.LogDeathProbability = "no person damage";

                        _damageExtent.PersonDamage = 0;
                        _damageExtent.LogPersonDamage = "no person damage";
                    }
                    else
                    {

                        _damageExtent.Length = mapObj.geometry.Length;

                        double _deaths = _prA.Value * _mergedObjParam.Personcount *
                                        (double)_mergedObjParam.NumberOfVehicles * _damageExtent.Length / (double)_mergedObjParam.Velocity / 24000.0d;

                        string _logDeaths1 = $"Deaths = prA * PersonCount * NumberOfVehicles * Length / Velocity / 24000";
                        string _logDeaths2 = $"Deaths = {_prA.Value:F3} * {_mergedObjParam.Personcount} * {_mergedObjParam.NumberOfVehicles} * {_damageExtent.Length:F3} / {_mergedObjParam.Velocity} / 24000";

                        switch (_intensityDegree)
                        {
                            case 0:
                                _deaths *= _objectParamProcess.MortalityHigh;
                                _logDeaths1 += $" * MortalityHigh";
                                _logDeaths2 += $" * {_objectParamProcess.MortalityHigh:F3}";
                                break;
                            case 1:
                                _deaths *= _objectParamProcess.MortalityMedium;
                                _logDeaths1 += $" * MortalityMedium";
                                _logDeaths2 += $" * {_objectParamProcess.MortalityMedium:F3}";
                                break;
                            case 2:
                                _deaths *= _objectParamProcess.MortalityLow;
                                _logDeaths1 += $" * MortalityLow";
                                _logDeaths2 += $" * {_objectParamProcess.MortalityLow:F3}";
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                        }

                        _damageExtent.Deaths = _deaths;
                        _damageExtent.LogDeaths = _logDeaths1 + ";\n" + _logDeaths2;

                        int _passagesSamePerson = 4; //TODO: HARD CODED
                        double _deathProbability = _deaths * _passagesSamePerson / _mergedObjParam.NumberOfVehicles / _mergedObjParam.Personcount;
                        string _logDProb1 = $"IndivudualDeathRisk = Deaths * PassagesSamePerson / NumberOfVehicles / PersonCount";
                        string _logDProb2 = $"IndivudualDeathRisk = {_deaths:F6} * {_passagesSamePerson} / {_mergedObjParam.NumberOfVehicles} / {_mergedObjParam.Personcount}";
                        _damageExtent.DeathProbability = _deathProbability;
                        _damageExtent.LogDeathProbability = _logDProb1 + ";\n" + _logDProb2;

                        _damageExtent.PersonDamage = _deaths * WillingnessToPay;
                        _damageExtent.LogPersonDamage = $"PersonDamage = Deaths * WillingnessToPay;\n";
                        _damageExtent.LogPersonDamage += $"PersonDamage = {_deaths:F6} * {WillingnessToPay:C}";
                    }
                }
                // TOWERS
                // POINT OBJECT
                else if (_mergedObjParam.FeatureType == 0)
                {
                    _damageExtent.Deaths = 0;
                    _damageExtent.LogDeaths = "no person damage";

                    _damageExtent.DeathProbability = 0;
                    _damageExtent.LogDeathProbability = "no person damage";

                    _damageExtent.PersonDamage = 0;
                    _damageExtent.LogPersonDamage = "no person damage";
                }
                // POLYGON NOT IMPLEMENTED
                else
                {
                    _damageExtent.Log += $"ERROR: Feature type not implemented";
                    return _damageExtent;
                }


                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PROPERTY DAMAGE

                double _propertyDamage = _prA.Value * _mergedObjParam.Value;
                string _logPropertyDamage1 = $"PropertyDamage = prA * Value";
                string _logPropertyDamage2 = $"PropertyDamage = {_prA.Value:F3} * {_mergedObjParam.Value:C}";

                switch ((int)_mergedObjParam.FeatureType)
                {
                    case 0: //POINT BASED OBJECT (like towers)
                        _damageExtent.Piece = 1;
                        _logPropertyDamage1 += $" * Piece";
                        _logPropertyDamage2 += $" * 1";
                        break;

                    case 1: //LINE BASED OBJECT (like streets)
                        _damageExtent.Length = mapObj.geometry.Length;
                        _propertyDamage *= mapObj.geometry.Length;
                        _logPropertyDamage1 += $" * Length";
                        _logPropertyDamage2 += $" * {mapObj.geometry.Length:F3}";
                        break;

                    default:
                        _damageExtent.Log += $"ERROR: Infrastructure, FEATURETYPE = {_mergedObjParam.FeatureType} \n";
                        return _damageExtent;
                }

                switch (_intensityDegree)
                {
                    case 0:
                        _propertyDamage *= _objectParamProcess.VulnerabilityHigh;
                        _logPropertyDamage1 += $" * VulnerabilityHigh";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityHigh:F3}";
                        break;
                    case 1:
                        _propertyDamage *= _objectParamProcess.VulnerabilityMedium;
                        _logPropertyDamage1 += $" * VulnerabilityMedium";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityMedium:F3}";
                        break;
                    case 2:
                        _propertyDamage *= _objectParamProcess.VulnerabilityLow;
                        _logPropertyDamage1 += $" * VulnerabilityLow";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityLow:F3}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                }

                _damageExtent.PropertyDamage = _propertyDamage;
                _damageExtent.LogPropertyDamage = _logPropertyDamage1 + ";\n" + _logPropertyDamage2;

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            }
            // AGRICULTURE
            else if (_mergedObjParam.ObjectClass.ID == 4)
            {
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PERSON DAMAGE

                _damageExtent.Deaths = 0;
                _damageExtent.LogDeaths = "no person damage";

                _damageExtent.DeathProbability = 0;
                _damageExtent.LogDeathProbability = "no person damage";

                _damageExtent.PersonDamage = 0;
                _damageExtent.LogPersonDamage = "no person damage";

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                // PROPERTY DAMAGE

                double _propertyDamage = _prA.Value * _mergedObjParam.Value; //value per hectare!!!
                string _logPropertyDamage1 = $"PropertyDamage = prA * Value";
                string _logPropertyDamage2 = $"PropertyDamage = {_prA.Value:F3} * {_mergedObjParam.Value:C}";

                switch ((int)_mergedObjParam.FeatureType)
                {
                    case 2: //POLYGON BASED OBJECT
                        _damageExtent.Area = mapObj.geometry.Area;
                        _propertyDamage *= _damageExtent.Area / 10000.0;  //in hectare!
                        _logPropertyDamage1 += $" * Area / 10000";
                        _logPropertyDamage2 += $" * {(_damageExtent.Area / 10000.0):F3}";
                        break;

                    default:
                        _damageExtent.Log += $"ERROR: Agriculture, FEATURETYPE = {_mergedObjParam.FeatureType} \n";
                        return _damageExtent;
                }

                switch (_intensityDegree)
                {
                    case 0:
                        _propertyDamage *= _objectParamProcess.VulnerabilityHigh;
                        _logPropertyDamage1 += $" * VulnerabilityHigh";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityHigh:F3}";
                        break;
                    case 1:
                        _propertyDamage *= _objectParamProcess.VulnerabilityMedium;
                        _logPropertyDamage1 += $" * VulnerabilityMedium";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityMedium:F3}";
                        break;
                    case 2:
                        _propertyDamage *= _objectParamProcess.VulnerabilityLow;
                        _logPropertyDamage1 += $" * VulnerabilityLow";
                        _logPropertyDamage2 += $" * {_objectParamProcess.VulnerabilityLow:F3}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                }

                _damageExtent.PropertyDamage = _propertyDamage;
                _damageExtent.LogPropertyDamage = _logPropertyDamage1 + ";\n" + _logPropertyDamage2;

                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            }

            else
            {
                _damageExtent.Log += $"ERROR: OBJECT CLASS = {_mergedObjParam.ObjectClass.ID} \n";
                return _damageExtent;
            }

            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            // RESILIENCE FACTOR
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            if (mapObj.ResilienceValues != null && mapObj.ResilienceValues.Any())
            {
                Tuple<double, string> _resilience = DamageExtentService.ComputeResilienceFactor(mapObj.ResilienceValues.ToList(), intensity);

                _damageExtent.ResilienceFactor = _resilience.Item1;
                _damageExtent.LogResilienceFactor = _resilience.Item2;
            }
            else
            {
                _damageExtent.ResilienceFactor = 0;
                _damageExtent.LogResilienceFactor = "no resilience available";
            }

            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            // INDIRECT DAMAGE
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            double _indirectDamage = _prA.Value * _objectParamProcess.Value;                                    //bug fix: 14.09.2021, chb
            string _logIndirectDamage1 = $"IndirectDamage = prA * Value";
            string _logIndirectDamage2 = $"IndirectDamage = {_prA.Value:F3} * {_objectParamProcess.Value:C}";

            if (_mergedObjParam.ObjectClass.ID != 3) //not available for infrastructure
            {
                switch (_intensityDegree) //0=high, 1=med, 2=low
                {
                    case 0:
                        _indirectDamage *= _objectParamProcess.DurationHigh;
                        _logIndirectDamage1 += $" * DurationHigh";
                        _logIndirectDamage2 += $" * {_objectParamProcess.DurationHigh:F0}";
                        break;
                    case 1:
                        _indirectDamage *= _objectParamProcess.DurationMedium;
                        _logIndirectDamage1 += $" * DurationMedium";
                        _logIndirectDamage2 += $" * {_objectParamProcess.DurationMedium:F0}";
                        break;
                    case 2:
                        _indirectDamage *= _objectParamProcess.DurationLow;
                        _logIndirectDamage1 += $" * DurationLow";
                        _logIndirectDamage2 += $" * {_objectParamProcess.DurationLow:F0}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_intensityDegree), _intensityDegree, "out of range");
                }

                //Building and Special Objects
                if (_mergedObjParam.ObjectClass.ID <= 2)
                {
                    // staff property indicates if indirect damage is available
                    if (_mergedObjParam.HasProperties.Where(m => m.Property == nameof(_mergedObjParam.Staff)).Any())
                    {
                        _indirectDamage *= (double)_mergedObjParam.Staff;
                        _logIndirectDamage1 += $" * Staff";
                        _logIndirectDamage2 += $" * {_mergedObjParam.Staff:F0}";

                        if (_damageExtent.Clipped)
                        {
                            _indirectDamage *= _damageExtent.Part;
                            _logIndirectDamage1 += $" * PartOfStaff";
                            _logIndirectDamage2 += $" * {_damageExtent.Part:F2}";
                        }

                        if (_mergedObjParam.Staff <= 0)
                        {
                            _damageExtent.Log += $"{ResModel.DE_Staff} = {_mergedObjParam.Staff} \n";
                        }
                    }
                    else // Buildings without indirect damage
                    {
                        _indirectDamage = 0;
                        _logIndirectDamage1 = $"no indirect damage";
                        _logIndirectDamage2 = "";
                    }
                }
                //Agriculture
                else if (_mergedObjParam.ObjectClass.ID == 4)
                {
                    _indirectDamage *= _damageExtent.Area / 10000.0;   //in hectare!
                    _logIndirectDamage1 += $" * Area / 10000";
                    _logIndirectDamage2 += $" * {_damageExtent.Area / 10000.0:F3}";
                }

            }
            else //Infrastructure
            {
                _indirectDamage = 0;
                _logIndirectDamage1 = $"no indirect damage";
                _logIndirectDamage2 = "";
            }

            _damageExtent.IndirectDamage = _indirectDamage;
            _damageExtent.LogIndirectDamage = _logIndirectDamage1 + ";\n" + _logIndirectDamage2;

            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            // if no errors reported
            if (String.IsNullOrWhiteSpace(_damageExtent.Log))
                _damageExtent.Log = "OK \n";

            return _damageExtent;
        }

        /// <summary>
        /// Delete all damage extents of project in the database
        /// </summary>
        /// <param name="projectId"></param>
        public async Task DeleteDamageExtentsFromDBAsync(int projectId)
        {
            if (projectId < 1)
                return;

            string _deleteQueryString =
                $"delete " +
                $"from \"DamageExtent\" " +
                $"where \"DamageExtent\".\"MappedObjectId\" in " +
                $"(select damageextent.\"MappedObjectId\" " +
                $"from \"DamageExtent\" as damageextent, \"Intensity\" as intensity " +
                $"where intensity.\"ProjectId\" = {projectId} " +
                $"and damageextent.\"IntensityId\" = intensity.\"ID\") ";

            using (var transaction = _context.Database.BeginTransaction())
            {
                await _context.Database.ExecuteSqlRawAsync(_deleteQueryString);
                await transaction.CommitAsync();
            }

            //Change the project to state "Started"
            await ResultController.SetProjectStatusAsync(projectId, 1);
        }

        /// <summary>
        /// Save damage extents to db
        /// </summary>
        /// <param name="damageExtents"></param>
        public async Task SaveDamageExtentToDBAsync(List<DamageExtent> damageExtents, string logFile)
        {
            if (damageExtents == null || damageExtents.Count() == 0)
                return;

            Stopwatch _saveWatch = new Stopwatch();
            _saveWatch.Start();

            _logger.LogWarning($" + start: elapsed time = " + _saveWatch.Elapsed.ToString());
            _saveWatch.Restart();

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(
                async () =>
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        _logger.LogWarning($" + transaction start: elapsed time = " + _saveWatch.Elapsed.ToString());
                        _saveWatch.Restart();
                        try
                        {
                            int counter = 0;
                            foreach (DamageExtent item in damageExtents)
                            {
                                //item.IntensityId = item.Intensity.ID;
                                //item.Intensity = null;
                                //item.MappedObjectId = item.MappedObject.ID;
                                //item.MappedObject = null;

                                //_context.DamageExtents.Add(item);
                                _context.Entry(item).State = EntityState.Added;
                                counter++;
                                _safeFileWriter.WriteFile(logFile, $"({counter}/{damageExtents.Count}) Objeto {item.MappedObject.ID} agregado a la base de datos" + System.Environment.NewLine);
                            }

                            _logger.LogWarning($" + save: elapsed time = " + _saveWatch.Elapsed.ToString());
                            _saveWatch.Restart();

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                        }
                        _logger.LogWarning($" + commit: elapsed time = " + _saveWatch.Elapsed.ToString());
                        _saveWatch.Restart();
                    }

                });

            _logger.LogWarning($" + end: elapsed time = " + _saveWatch.Elapsed.ToString());
            _saveWatch.Stop();
        }

        /// <summary>
        /// Compose the project summary, no new computation perfomed, >WebApp<
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task<ProjectResult> ComposeProjectResultAsync(int projectId, bool details = false)
        {
            // Check if project exists
            bool _projectExist = await _context.Projects.Where(x => x.Id == projectId).AnyAsync();
            if (!_projectExist)
            {
                return new ProjectResult
                {
                    Project = new Project() { Id = projectId, Name = ResResult.PRJ_NoProject },
                    Message = String.Format(ResResult.PRJ_ProjectNotFound, projectId),
                };
            }

            IQueryable<DamageExtent> _damageExtents;
            _damageExtents = _context.DamageExtents
                            .Include(lm => lm.MappedObject)
                            .Where(d => d.MappedObject.Project.Id == projectId);
            //.OrderBy(de=>de.MappedObject.Objectparameter.ObjectClass.ID)
            //.OrderBy(de=>de.Intensity.ID)
            //.ToListAsync();

            if (!_damageExtents.Any())
            {
                return new ProjectResult
                {
                    Project = await _context.Projects.FindAsync(projectId) ?? new Project() { Id = projectId, Name = ResResult.PRJ_NoProject },
                    //Message = $"\n{ResResult.PRJ_NoDamageExtent}",
                };
            }

            Project _project = _damageExtents.First().MappedObject.Project;

            IList<Intensity> _intensityListRaw = _context.Intensities
                .Where(i => i.Project.Id == projectId)
                .ToList<Intensity>();

            List<NatHazard> _hazards = _intensityListRaw.Select(i => i.NatHazard).Distinct().OrderBy(n => n.ID).ToList();

            var _projectResult = new ProjectResult()
            {
                Project = _project,
                NatHazards = _hazards,
                ShowDetails = details,
            };

            // loop over natural hazards
            foreach (NatHazard hazard in _hazards)
            {
                //List<bool> beforeActions = _damageExtents.
                //    Where(de => de.Intensity.NatHazard == hazard)
                //    .Select(de => de.Intensity.BeforeAction)
                //    .Distinct()
                //    .OrderByDescending(a => a).ToList();

                List<bool> beforeActions = _intensityListRaw
                    .Where(i => i.NatHazard.ID == hazard.ID)
                    .Select(i => i.BeforeAction)
                    .Distinct()
                    .OrderByDescending(a => a).ToList();

                foreach (bool beforeMeasure in beforeActions)
                {
                    //List<IKClasses> ikClasses = _damageExtents
                    //    .Where(de => de.Intensity.NatHazard == hazard)
                    //    .Where(de => de.Intensity.BeforeAction == beforeMeasure)
                    //    .Select(de => de.Intensity.IKClasses)
                    //    .Distinct()
                    //    .OrderBy(p => p.Value).ToList();

                    List<IKClasses> ikClasses = _intensityListRaw
                        .Where(i => i.NatHazard.ID == hazard.ID)
                        .Where(i => i.BeforeAction == beforeMeasure)
                        .Select(i => i.IKClasses)
                        .Distinct()
                        .OrderBy(p => p.Value).ToList();

                    var _processResult = new ProcessResult()
                    {
                        NatHazard = hazard,
                        BeforeAction = beforeMeasure,
                    };
                    _projectResult.ProcessResults.Add(_processResult);

                    foreach (IKClasses period in ikClasses)
                    {
                        List<DamageExtent> _scenarioDamageExtents = _damageExtents
                            .Where(de => de.Intensity.BeforeAction == beforeMeasure && de.Intensity.NatHazard == hazard && de.Intensity.IKClasses == period)
                            .OrderBy(de => de.MappedObject.Objectparameter.ObjectClass.ID)
                            .ThenBy(de => de.MappedObject.ID)
                            .ToList();

                        var _scenarioResult = new ScenarioResult()
                        {
                            DamageExtents = _scenarioDamageExtents,
                            NatHazard = hazard,
                            BeforeAction = beforeMeasure,
                            IkClass = period,
                        };

                        _processResult.ScenarioResults.Add(_scenarioResult);
                    }
                }
            }

            //////////////////////////////////////////////////////////////////////////////////

            // Protection Measure
            var _protectionMeasure = _project.ProtectionMeasure;
            if (_protectionMeasure != null)
            {
                _projectResult.ProtectionMeasure = _protectionMeasure;
                if (_projectResult.ProtectionMeasure.LifeSpan < 1)
                {
                    _projectResult.Message += $"\n{String.Format(ResResult.PRJ_LifeSpanError, _projectResult.ProtectionMeasure.LifeSpan)}";
                }
            }

            // Project Summary

            // Errors Summary
            var _damageExtentErrors = _damageExtents
                .Where(de => de.Log.Trim().ToUpper() != "OK")
                .OrderBy(de => de.MappedObject.ID)
                .ThenByDescending(de => de.Intensity.BeforeAction)
                .ThenBy(de => de.Intensity.NatHazard.ID)
                .ThenBy(de => de.Intensity.IKClasses.Value)
                .ThenBy(de => de.Intensity.IntensityDegree)
                .Select(de => new DamageExtentError
                {
                    MappedObject = de.MappedObject,
                    Intensity = de.Intensity,
                    Issue = de.Log
                }
                );
            if (_damageExtentErrors.Any())
            {
                _projectResult.DamageExtentErrors = _damageExtentErrors.ToList();
            }

            return _projectResult;
        }

        #region IntensityService

        /// <summary>
        /// get prA from intensity
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public PrA GetPrA(Intensity intensity)
        {
            PrA pra = intensity.Project?.PrAs
                .Where(p => p.IKClasses.ID == intensity.IKClasses.ID && p.NatHazard.ID == intensity.NatHazard.ID)
                .SingleOrDefault();

            return pra;
        }

        /// <summary>
        /// Clip intensity to project perimeter, fix geometry for intersection & difference
        /// Attention: "side effect" = geometry of intensity changes
        /// </summary>
        /// <param name="intens"></param>
        public void ClipToProject(Intensity intens)
        {
            // trick to make all geometries valid for Intersection / Difference: Buffer(0.001)  <<<<<<<<<<<<<<<<<<<<<<
            intens.geometry = GeometryTools.Polygon2Multipolygon(intens.geometry.Buffer(0.001));

            //intensity can't be outside of perimeter
            Geometry clippedIntens = intens.Project?.geometry.Intersection(intens.geometry);

            //keep multipolygon geometry
            clippedIntens = GeometryTools.Polygon2Multipolygon(clippedIntens.Buffer(0.001));

            //area difference due to intersection
            double areaDifference = intens.geometry.Area - clippedIntens.Area;

            Debug.WriteLine($"PROJECT CLIPPING: Intensity {intens.ID}: area diff = {areaDifference:F2}, new area = {clippedIntens.Area:F2}, type: {intens.geometry.GeometryType} -> {clippedIntens.GeometryType}");

            //assign new geometry
            intens.geometry = (MultiPolygon)clippedIntens;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId">project id</param>
        /// <param name="hazardId">hazard id</param>
        /// <param name="periodId">period id</param>
        /// <param name="beforeAction">beforeAction boolean</param>
        /// <returns></returns>
        public async Task<List<Intensity>> GetIntensityMap(int projectId, int hazardId, int periodId, bool beforeAction)
        {
            try
            {
                IList<Intensity> intensityListRaw = await _context.Intensities
                    .Where(i => i.Project.Id == projectId &&
                           i.NatHazard.ID == hazardId &&
                           i.BeforeAction == beforeAction &&
                           i.IKClasses.ID == periodId)
                    .ToListAsync<Intensity>();

                if (intensityListRaw == null || intensityListRaw.Count == 0)
                {
                    //Debug.WriteLine($"Warning @ {nameof(getIntensityMap)}: No Intensities found for project id {projectId}, hazard {hazardId}, period {periodId}, beforeAction {beforeAction}");
                    return null;
                }

                //avoiding side effects: copying intensities
                IList<Intensity> intensityList = new List<Intensity>();
                foreach (Intensity intens in intensityListRaw)
                {
                    var intensCopy = (Intensity)intens.Clone();
                    intensityList.Add(intensCopy);
                }

                //clip to project and fix geometries
                foreach (Intensity intens in intensityList)
                {
                    ClipToProject(intens);
                }

                if (intensityList.Count > 1) //2 or more intensities available
                {
                    // intensityDegree: 0=high, 1=medium, 2=low
                    Intensity high = intensityList.OrderBy(i => i.IntensityDegree).First();
                    Intensity medium = intensityList.OrderBy(i => i.IntensityDegree).Skip(1).First();

                    // compute geometry of "medium" that is not in "high"
                    var clip1 = medium.geometry.Difference(high.geometry);

                    ////area difference due to difference
                    //double areaDifference = medium.geometry.Area - clip1.Area;
                    //Debug.WriteLine($"INTENSITY CLIPPING: Intensity {medium.ID}: area diff = {areaDifference:F2}, new area = {clip1.Area:F2}");

                    // save geometry back to "medium"
                    medium.geometry = GeometryTools.Polygon2Multipolygon(clip1.Buffer(0.001));

                    if (intensityList.Count > 2)
                    {
                        Intensity low = intensityList.OrderBy(i => i.IntensityDegree).Skip(2).First();

                        ////DEBUG
                        ////if (projectId == 5 && periodId == 3 && beforeAction && hazardId == 2)
                        ////{
                        ////    var s = "stop";
                        ////    //continue;
                        ////}
                        //var cl21 = low.geometry.Difference(medium.geometry);
                        //var cl22 = cl21.Difference(high.geometry);

                        var clip2 = low.geometry.Difference(medium.geometry).Difference(high.geometry);

                        ////area difference due to difference
                        //areaDifference = low.geometry.Area - clip2.Area;
                        //Debug.WriteLine($"INTENSITY CLIPPING: Intensity {low.ID}: area diff = {areaDifference:F2}, new area = {clip2.Area:F2}");

                        low.geometry = GeometryTools.Polygon2Multipolygon(clip2.Buffer(0.001));
                    }
                }
                // Clipping
                //IGeometry clip1 = p1.Difference(p0);
                //IGeometry clip2 = p2.Difference(p1).Difference(p0);

                return intensityList.OrderBy(i => i.IntensityDegree).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region MappedObjectService

        public static Tuple<double, string> ComputeResilienceFactor(List<ResilienceValues> allResilienceList, Intensity intensity)
        {
            NatHazard natHazard = intensity.NatHazard;
            bool beforeAction = intensity.BeforeAction; //--> to distinguish the before/after resilience

            int _resilienceHazardID;
            if (natHazard?.ID == 1) // Sequia
            {
                _resilienceHazardID = 1;
            }
            else //other nathazards
            {
                _resilienceHazardID = 2;
            }

            // use only resilience values of given nathazard
            List<ResilienceValues> _resilienceList = allResilienceList
                .Where(r => r.ResilienceWeight.NatHazard.ID == _resilienceHazardID && r.ResilienceWeight.BeforeAction == beforeAction).ToList();

            List<string> _logString1 = new List<string>();
            List<string> _logString2 = new List<string>();
            List<string> _logIDStrings = new List<string>();

            //if list is empty: factor = 0
            if (_resilienceList == null || !_resilienceList.Any())
            {
                return new Tuple<double, string>(0.0d, "no resiliece values found");
            }

            double _sumWeight = 0;
            double _sumValueWeight = 0;

            foreach (ResilienceValues item in _resilienceList)
            {
                _sumValueWeight += item.Value * item.Weight;
                var string1 = $" ({item.Value} * {item.Weight}) ";
                _logString1.Add(string1);

                _sumWeight += item.Weight;
                var string2 = $" {item.Weight} ";
                _logString2.Add(string2);

                _logIDStrings.Add(item.ResilienceWeight.ResilienceFactor.ID + $" V{item.Value:F2} W{item.Weight:F1}");
            }

            string _logResilienceFactor = $"ResilienceFactor (c{_resilienceHazardID}) = " +
                string.Join("+", _logString1) + " / (" + string.Join("+", _logString2) + ")" +
                ";\n         ResilienceValues: " + string.Join("; \n", _logIDStrings); ;

            if (_sumWeight == 0) //avoid division by 0
            {
                return new Tuple<double, string>(0.0d, _logResilienceFactor);
            }
            else
            {
                double _resilienceFactor = _sumValueWeight / _sumWeight;

                var _result = new Tuple<double, string>(_resilienceFactor, _logResilienceFactor);
                return _result;
            }
        }

        /// <summary>
        /// all DamagePotentials having geometry in project.geometry, including crossing project perimeter
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task<List<MappedObject>> GetAllDamagePotentials(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);

            List<MappedObject> dpList = await _context.MappedObjects
                //.Include(mo => mo.Objectparameter)
                //.Include(mo => mo.FreeFillParameter)
                .Where(m => m.Project.Id == projectId)
                .ToListAsync<MappedObject>();

            //all DamagePotentials having geometry in project.geometry, including crossing project perimeter
            var dpListFiltered = dpList.Where(m => m.Project.geometry.Intersects(m.geometry));

            return dpListFiltered.ToList();
        }

        /// <summary>
        /// DamagePotential within intensity.geometry
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="dpList"></param>
        /// <returns></returns>
        public async Task<List<MappedObject>> GetDamagePotentialsWithin(Intensity intensity, IList<MappedObject> dpList)
        {
            var project = await _context.Projects.FindAsync(intensity.Project.Id);

            var dpListFiltered = dpList.Where(m => m.geometry.Within(intensity.geometry));

            List<MappedObject> withinList = new List<MappedObject>();

            foreach (var damagePotential in dpListFiltered)
            {
                var copyDamagePotential = (MappedObject)damagePotential.Clone();

                copyDamagePotential.IsClipped = false;
                copyDamagePotential.Intensity = (Intensity)intensity.Clone(); //remember the intensity the mappedObject is in

                withinList.Add(copyDamagePotential);
            }

            return withinList;
        }

        /// <summary>
        /// DamagePotential crossing intensity.geometry
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="dpList"></param>
        /// <returns></returns>
        public async Task<List<MappedObject>> GetDamagePotentialCrossing(Intensity intensity, IList<MappedObject> dpList)
        {
            //Stopwatch _timer = new Stopwatch();
            //_timer.Start();

            //            select "ID"
            //from "MappedObject" as mapObj
            //where ST_Intersects((select geometry from "Intensity" where "ID" = 117), mapObj.Geometry) AND
            //NOT ST_Contains((select geometry from "Intensity" where "ID" = 117), mapObj.Geometry);
            var project = await _context.Projects.FindAsync(intensity.Project.Id);

            //long ts1 = _timer.ElapsedMilliseconds; //////////////////////////////
            //_timer.Restart();

            var dpListFiltered = dpList
                .Where(m => !m.geometry.Within(intensity.geometry) &&
                             m.geometry.Intersects(intensity.geometry)
                             ).ToList();

            //long ts2 = _timer.ElapsedMilliseconds; //////////////////////////////
            //_timer.Restart();

            List<MappedObject> clippedList = new List<MappedObject>();

            foreach (var damagePotential in dpListFiltered)
            {
                //damagePotential can't be outside of intensity
                Geometry clippedDamagePotentialGeometry = intensity.geometry.Intersection(damagePotential.geometry);

                //#if DEBUG
                //                //area difference due to intersection
                //                double areaDifference = damagePotential.geometry.Area - clippedDamagePotentialGeometry.Area;
                //                Debug.WriteLine($"DAMAGEPOTENTIAL CLIPPING: DamagePotential {damagePotential.ID}: area diff = {areaDifference:F2}, new area = {clippedDamagePotentialGeometry.Area:F2}, type: {damagePotential.geometry.GeometryType} -> {clippedDamagePotentialGeometry.GeometryType}");
                //#endif

                //assign new geometry to new MappedObject
                MappedObject clippedDamagePotential = new MappedObject()
                {
                    FreeFillParameter = damagePotential.FreeFillParameter,
                    ID = damagePotential.ID,
                    Objectparameter = damagePotential.Objectparameter,
                    Project = damagePotential.Project,
                    geometry = clippedDamagePotentialGeometry,            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    IsClipped = true,                                     //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    Intensity = (Intensity)intensity.Clone(),             //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    ResilienceValues = damagePotential.ResilienceValues,
                };

                clippedList.Add(clippedDamagePotential);
            }

            //long ts3 = _timer.ElapsedMilliseconds; //////////////////////////////
            //_timer.Restart();

            //Logging.warn($"    111     {ts1:F2} - {ts2:F2} - {ts3:F3}");
            //_timer.Stop();

            return clippedList;
        }



        /// <summary>
        /// Delete all damage extents of this project in database and recreate them
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task CreateDamageExtent(int projectId, string logFile)
        {
            Stopwatch _stopWatch = new Stopwatch();
            _stopWatch.Start();

            //DBManager.NewSession(); //bac session

            //ConcurrentBag<DamageExtent> _saveDamageExtents = new ConcurrentBag<DamageExtent>();
            List<DamageExtent> _saveDamageExtents = new List<DamageExtent>();
            _safeFileWriter.WriteFile(logFile, $"El cálculo comienza" + System.Environment.NewLine);
            await this.DeleteDamageExtentsFromDBAsync(projectId);  //DELETE ALL
            _safeFileWriter.WriteFile(logFile, $"Datos ya calculados borrados de la base de datos" + System.Environment.NewLine);
            var _hazards = await _context.NatHazards.ToListAsync<NatHazard>();
            var _ikClasses = await _context.IntensitaetsKlassen.ToListAsync<IKClasses>();
            List<bool> _beforeActions = new List<bool>() { true, false };

            _logger.LogWarning($"querying: elapsed time = " + _stopWatch.Elapsed.ToString());
            _safeFileWriter.WriteFile(logFile, $"Datos cargados desde la base de datos" + System.Environment.NewLine);
            _stopWatch.Restart();

            var _damagePotentialController = this;
            var _allAffectedDamagePotentials = await _damagePotentialController.GetAllDamagePotentials(projectId); //unprocessed Damage Potentials in the project perimeter

            _logger.LogWarning($" " + _stopWatch.Elapsed.ToString() + $", count= {_allAffectedDamagePotentials.Count()}");
            _safeFileWriter.WriteFile(logFile, $"Cargar los potenciales de peligro de la base de datos. Cantidad = {_allAffectedDamagePotentials.Count()}" + System.Environment.NewLine);
            _stopWatch.Restart();

            foreach (var hazard in _hazards)
            {
                _safeFileWriter.WriteFile(logFile, $"Riesgos naturales {hazard.Name}" + System.Environment.NewLine);
                foreach (var period in _ikClasses)
                {
                    _safeFileWriter.WriteFile(logFile, $"Periodo {period.Description}" + System.Environment.NewLine);

                    foreach (var beforeAction in _beforeActions)
                    {
                        if (beforeAction) _safeFileWriter.WriteFile(logFile, $"Cálculo del riesgo antes de la medida" + System.Environment.NewLine);
                        else if (beforeAction) _safeFileWriter.WriteFile(logFile, $"Cálculo del riesgo despuiés de la medida" + System.Environment.NewLine);


                        _stopWatch.Restart();

                        List<Intensity> _intensities = await this.GetIntensityMap(projectId, hazard.ID, period.ID, beforeAction);
                        List<MappedObject> _allProcessedDamagePotentials = new List<MappedObject>();

                        _logger.LogWarning($"getInsityMap: elapsed time = " + _stopWatch.Elapsed.ToString());
                        _stopWatch.Restart();

                        if (_intensities == null || _intensities.Count() == 0)
                            continue;
                        _safeFileWriter.WriteFile(logFile, $"Cálculo del riesgo natural {hazard.Name} y la intensidad {period.Description}" + System.Environment.NewLine);

                        Stopwatch _damageWatch = new Stopwatch();
                        _damageWatch.Start();

                        // gather all processed DamagePotentials
                        foreach (var intensity in _intensities)
                        {
                            _damageWatch.Restart();

                            IList<MappedObject> dpListWithin = await _damagePotentialController.GetDamagePotentialsWithin(intensity, _allAffectedDamagePotentials);
                            List<MappedObject> outlist = dpListWithin.ToList();

                            _logger.LogWarning($">getDamagePotentialsWithin: elapsed time = " + _damageWatch.Elapsed.ToString());
                            
                            _damageWatch.Restart();

                            //IList<MappedObject> dpListWithin2 = _damagePotentialController.getDamagePotentialsWithin2(intensity, projectId);
                            //List<MappedObject> outlist = dpListWithin2.ToList();
                            //Logging.warn($">getDamagePotentialsWithin2: elapsed time = " + _damageWatch.Elapsed.ToString());
                            //_damageWatch.Restart();

                            IList<MappedObject> dpListCrossing = await _damagePotentialController.GetDamagePotentialCrossing(intensity, _allAffectedDamagePotentials);
                            outlist.AddRange(dpListCrossing); //Merge Within and Crossing for Intensity

                            _logger.LogWarning($">getDamagePotentialCrossing: elapsed time = " + _damageWatch.Elapsed.ToString());
                            _safeFileWriter.WriteFile(logFile, $"Mezcla de todos los potenciales de peligro dentro de la intensidad con ID {intensity.ID}" + System.Environment.NewLine);
                            _damageWatch.Restart();

                            //IList<MappedObject> dpListCrossing2 = _damagePotentialController.getDamagePotentialCrossing2(intensity, projectId);
                            //outlist.AddRange(dpListCrossing2); //Merge Within and Crossing for Intensity
                            //Logging.warn($">getDamagePotentialCrossing2: elapsed time = " + _damageWatch.Elapsed.ToString());
                            //_damageWatch.Restart();

                            _allProcessedDamagePotentials.AddRange(outlist); //collect all processed DamagePotentials
                        }
                        _damageWatch.Stop();

                        _logger.LogWarning($"getDamagePotential: elapsed time = " + _stopWatch.Elapsed.ToString());
                        
                        _stopWatch.Restart();

                        //Parallel.ForEach(_intensities,
                        //    (intensity) =>
                        //    {
                        int totalDamageCounter = _allProcessedDamagePotentials.Count;
                        _safeFileWriter.WriteFile(logFile, $"Potenciales totales por calcular: {totalDamageCounter})" + System.Environment.NewLine);
                        int currentDamageCounter = 1;
                        foreach (var intensity in _intensities)
                        {

                            var outlist = _allProcessedDamagePotentials.Where(o => o.Intensity.ID == intensity.ID).ToList();

                            List<DamageExtent> outDamageExtents = new List<DamageExtent>();
                            //ConcurrentBag<DamageExtent> outDamageExtents = new ConcurrentBag<DamageExtent>();

                            //Parallel.ForEach(outlist, (damagePotential) =>
                            //{
                             foreach (MappedObject damagePotential in outlist)
                            {
                                DamageExtent damageExtent = await this.ComputeDamageExtent(damagePotential, intensity);

                                if (damageExtent != null)
                                {
                                    outDamageExtents.Add(damageExtent);
                                    _safeFileWriter.WriteFile(logFile, $"({currentDamageCounter} / {totalDamageCounter}) Registro para ID {damageExtent.MappedObject.ID} {damageExtent.Log}" + System.Environment.NewLine);
                                }

                                currentDamageCounter++;
                            }
                            //});

                            _saveDamageExtents.AddRange(outDamageExtents);
                            //_saveDamageExtents.AddRange<DamageExtent>(outDamageExtents);

                            //double _sumPersonDamage = outDamageExtents.Sum(x => x.PersonDamage);
                            //double _sumDeaths = outDamageExtents.Sum(x => x.Deaths);
                            //double _sumProptertyDamage = outDamageExtents.Sum(x => x.PropertyDamage);

                            //Logging.warn($"  #DamageExtent: {outlist.Count}");

                        } //loop over intensities
                          //});

                        _logger.LogWarning($"computeDamageExtent: elapsed time = " + _stopWatch.Elapsed.ToString());
                        _safeFileWriter.WriteFile(logFile, $"Todos los potenciales calculados" + System.Environment.NewLine);
                        _stopWatch.Restart();

                    }//loop over actions
                }//loop over period

            }//loop over hazard

            _logger.LogWarning($"inbetween: elapsed time = " + _stopWatch.Elapsed.ToString());
            _stopWatch.Restart();

            _safeFileWriter.WriteFile(logFile, $"Los resultados se guardan en una base de datos" + System.Environment.NewLine);
            //one time saving to db <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            await this.SaveDamageExtentToDBAsync(_saveDamageExtents, logFile); //SAVE

            _logger.LogWarning($"saveDamageExtentToDB: elapsed time = " + _stopWatch.Elapsed.ToString());
            _safeFileWriter.WriteFile(logFile, $"Los resultados se guardaron en la base de datos" + System.Environment.NewLine); 
            _stopWatch.Restart();

            _stopWatch.Stop();


            //Change the project to state "Calculated"
            await ResultController.SetProjectStatusAsync(projectId, 2);

        }




        #endregion
    }
}
