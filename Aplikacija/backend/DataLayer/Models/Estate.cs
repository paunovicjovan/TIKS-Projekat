namespace DataLayer.Models;

public class Estate
{
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required double Price { get; set; }
    public required int SquareMeters  { get; set; }
    public required int TotalRooms { get; set; }
    public required EstateCategory Category { get; set; }
    public int? FloorNumber { get; set; }
    public required List<string> Images { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    
    // VEZE

    [BsonRepresentation(BsonType.ObjectId)]
    public required string UserId { get; set; } // ID usera koji je postavio nekretninu

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> PostIds { get; set; } = new(); // Lista ID-ova postova o nekretnini

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> FavoritedByUsersIds { get; set; } = new();  // Lista ID-ova korisnika koji su oznacili nekretninu kao omiljenu
}
