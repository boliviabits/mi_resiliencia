using NetTopologySuite.Geometries;

namespace MiResiliencia.Helpers.API
{
    public class GeometryTools
    {
        ///// <summary>
        ///// Convert hex-coded WKB to geometry object
        ///// </summary>
        ///// <param name="hexString"></param>
        ///// <returns></returns>
        //public static IGeometry WKBhex2Geom(string hexString)
        //{
        //    if (string.IsNullOrWhiteSpace(hexString))
        //        return null;

        //    try
        //    {
        //        var reader = new WKBReader();
        //        var geometryBinary = WKBReader.HexToBytes(hexString);
        //        IGeometry result = reader.Read(geometryBinary);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public static string Geom2WKBhex(IGeometry geometry)
        //{
        //    // writing WKB (Well Known Binary)
        //    var writer = new WKBWriter(GeoAPI.IO.ByteOrder.BigEndian, true);

        //    var wkbBin = writer.Write(geometry);

        //    // writing hex-coded WKB, used in POSTGIS DB geometery column
        //    string hexString = WKBWriter.ToHex(wkbBin);

        //    return hexString;
        //}

        //public static string Geom2GeoJSON(IGeometry geometry)
        //{
        //    var geoWriter = new GeoJsonWriter();

        //    var geoJSON = geoWriter.Write(geometry);

        //    return geoJSON;
        //}

        /// <summary>
        /// CONVERSION of geometry: Polygon to Multipolygon
        /// </summary>
        public static MultiPolygon Polygon2Multipolygon(Geometry polygon)
        {
            if (polygon is Polygon)
            {
                var factory = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 3857);
                Polygon[] polygons = new Polygon[] { polygon as Polygon };
                MultiPolygon multiPolygon = factory.CreateMultiPolygon(polygons);

                return multiPolygon;
            }
            else if (polygon is MultiPolygon)
            {
                return (MultiPolygon)polygon;
            }
            else
            {
                throw new ArgumentException($"Error: GeometryType not supported! input is of type {polygon.GeometryType}");
            }
        }
    }
}