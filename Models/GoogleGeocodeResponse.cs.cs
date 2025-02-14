using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GoogleGeocodeResponse
{
    [JsonPropertyName("results")]
    public List<GeocodeResult> Results { get; set; } = new();
}

public class GeocodeResult
{
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }

    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }

    [JsonPropertyName("types")]
    public List<string> Types { get; set; }
}

public class Geometry
{
    [JsonPropertyName("location")]
    public Location Location { get; set; }
}

public class Location
{
    [JsonPropertyName("lat")]
    public decimal Lat { get; set; }

    [JsonPropertyName("lng")]
    public decimal Lng { get; set; }
}
