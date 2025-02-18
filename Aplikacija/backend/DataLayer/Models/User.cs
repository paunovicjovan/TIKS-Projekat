namespace DataLayer.Models;

public class User
{
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; set; }
    public required string PasswordHash { get; init; }
    public UserRole Role { get; init; }

    // VEZE

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> EstateIds { get; set; } = new(); // Lista ID-a nekretnina koje je korisnik objavio

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> PostIds { get; set; } = new(); // Lista ID-a objava korisnika na forumu

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CommentIds { get; set; } = new(); // Lista ID-a komentara koje je korisnik napisao

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> FavoriteEstateIds { get; set; } = new(); // Lista ID-a omiljenih nekretnina korisnika
}