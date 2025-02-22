namespace DataLayer.Interfaces;

public interface IUserService
{
    public Task<Result<AuthResponseDTO, ErrorMessage>> Register(CreateUserDTO userDto);
    public Task<Result<AuthResponseDTO, ErrorMessage>> Login(LoginRequestDTO request);
    public Result<string, ErrorMessage> GetCurrentUserId(ClaimsPrincipal? user);
    public Task<Result<UserResultDTO, ErrorMessage>> GetById(string id);
    public Task<Result<UserResultDTO, ErrorMessage>> Update(string userId, UpdateUserDTO userDto);
    public Task<Result<bool, ErrorMessage>> AddCommentToUser(string userId, string commentId);
    public Task<Result<bool, ErrorMessage>> RemoveCommentFromUser(string userId, string commentId);
    public Task<Result<bool, ErrorMessage>> AddPostToUser(string userId, string postId);
    public Task<Result<bool, ErrorMessage>> RemovePostFromUser(string userId, string postId);
    public Task<Result<bool, ErrorMessage>> AddFavoriteEstate(string userId, string estateId);
    public Task<Result<bool, ErrorMessage>> RemoveFavoriteEstate(string userId, string estateId);
    public Task<Result<bool, ErrorMessage>> CanAddToFavorite(string userId, string estateId);
}
