using System.Text.Json.Serialization;

namespace backend.Models;

/*
    Destination represents a travel destination with various attributes
    including location details and user association.
*/
public class Destination
{
    public int Id { get; set; }
    public string City { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? Notes { get; set; }
    public bool Visited { get; set; } = false;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /* Foreign key to ApplicationUser */
    [JsonIgnore]
    public string? UserId { get; set; } = null!; // non-nullable

    /* Navigation property */
    [JsonIgnore]
    public ApplicationUser? User { get; set; } = null!;
}
