namespace DataLayer.Interfaces;

public interface ICommentService
{
    Task<Result<CommentResultDTO, ErrorMessage>> CreateComment(CreateCommentDTO commentDto, string userId);

    Task<Result<PaginatedResponseDTO<CommentResultDTO>, ErrorMessage>> GetCommentsForPost(string postId, int skip = 0,
        int limit = 10);

    Task<Result<CommentResultDTO, ErrorMessage>> UpdateComment(string commentId, UpdateCommentDTO commentDto);
    Task<Result<bool, ErrorMessage>> DeleteComment(string commentId);
}