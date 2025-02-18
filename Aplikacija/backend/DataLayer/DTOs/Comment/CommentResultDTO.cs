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
        Author = authorDoc != null
            ? new UserResultDTO
            {
                Id = authorDoc["_id"].AsObjectId.ToString(),
                Username = authorDoc["Username"].AsString,
                Email = authorDoc["Email"].AsString,
                PhoneNumber = authorDoc["PhoneNumber"].AsString,
                Role = (UserRole)authorDoc["Role"].AsInt32
            }
            : null!;
    }
}