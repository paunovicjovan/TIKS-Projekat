namespace DataLayer.Models;

public class Comment
{
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    // VEZE

    [BsonRepresentation(BsonType.ObjectId)]
    public required string AuthorId { get; set; } // ID korisnika koji je ostavio komentar

    [BsonRepresentation(BsonType.ObjectId)]
    public required string PostId { get; set; } // ID posta na koji se odnosi komentar
}
