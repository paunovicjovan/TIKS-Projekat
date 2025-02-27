namespace DataLayer.DTOs.Comment;

public class CommentResultDTO
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public required UserResultDTO Author { get; set; }

    public CommentResultDTO()
    {
    }

    [SetsRequiredMembers]
    public CommentResultDTO(BsonDocument comment)
    {
        var authorDoc = comment["AuthorData"].AsBsonArray.FirstOrDefault();

        Id = comment["_id"].AsObjectId.ToString();
        Content = comment["Content"].AsString;
        CreatedAt = comment["CreatedAt"].ToUniversalTime();
        Author = authorDoc != null ? new UserResultDTO(authorDoc) : null!;
    }
}