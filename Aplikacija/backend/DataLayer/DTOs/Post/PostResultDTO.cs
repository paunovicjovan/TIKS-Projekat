
namespace DataLayer.DTOs.Post;

public class PostResultDTO
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public required UserResultDTO Author { get; set; }
    public EstateResultDTO? Estate { get; set; }

    public PostResultDTO()
    {
        
    }
    
    [SetsRequiredMembers]
    public PostResultDTO(BsonDocument post)
    {
        var authorDoc = post["AuthorData"].AsBsonArray.FirstOrDefault();
        var estateDoc = post.Contains("EstateData") && 
                        !post["EstateData"].IsBsonNull ? post["EstateData"].AsBsonArray.FirstOrDefault() : null;

        Id = post["_id"].AsObjectId.ToString();
        Title = post["Title"].AsString;
        Content = post["Content"].AsString;
        CreatedAt = post["CreatedAt"].ToUniversalTime();
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
        Estate = estateDoc != null ? new EstateResultDTO(estateDoc) : null;
    }
}