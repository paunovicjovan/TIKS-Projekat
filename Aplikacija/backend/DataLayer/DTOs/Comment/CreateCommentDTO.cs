namespace DataLayer.DTOs.Comment;

public class CreateCommentDTO
{
    public required string Content { get; set; }
    public required string PostId { get; set; }
}