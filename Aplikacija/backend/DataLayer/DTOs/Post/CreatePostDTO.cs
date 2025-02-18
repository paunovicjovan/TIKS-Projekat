namespace DataLayer.DTOs.Post;

public class CreatePostDTO
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? EstateId { get; set; }
}