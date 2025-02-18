using DataLayer.DTOs.Post;

namespace DataLayer.Models;

public class Post
{
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; init; }

    // VEZE

    [BsonRepresentation(BsonType.ObjectId)]
    public required string AuthorId { get; set; } // ID autora objave

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CommentIds { get; set; } = new(); // Lista ID-ova komentara
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? EstateId { get; set; } // Opcioni ID nekretnine ako se post odnosi na neku nekretninu
}
