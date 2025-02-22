namespace DataLayer.Interfaces;

public interface IPostService
{
    Task<Result<PostResultDTO, ErrorMessage>> CreatePost(CreatePostDTO postDto, string userId);
    Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetAllPosts(string title = "", int page = 1, int pageSize = 10);
    Task<Result<PostResultDTO, ErrorMessage>> GetPostById(string postId);
    Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetAllPostsForEstate(string estateId, int page = 1, int pageSize = 10);
    Task<Result<bool, ErrorMessage>> UpdatePost(string postId, UpdatePostDTO postDto);
    Task<Result<bool, ErrorMessage>> DeletePost(string postId);
    Task<Result<bool, ErrorMessage>> AddCommentToPost(string postId, string commentId);
    Task<Result<bool, ErrorMessage>> RemoveCommentFromPost(string postId, string commentId);
    Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetUserPosts(string userId, int page = 1, int pageSize = 10);
}