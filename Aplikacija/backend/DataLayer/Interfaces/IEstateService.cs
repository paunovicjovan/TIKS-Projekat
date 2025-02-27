namespace DataLayer.Interfaces;

public interface IEstateService
{
    public Task<Result<List<Estate>, ErrorMessage>> GetAllEstatesFromCollection();
    public Task<Result<EstateResultDTO, ErrorMessage>> GetEstate(string id);
    public Task<Result<Estate, ErrorMessage>> CreateEstate(EstateCreateDTO newEstateDto, string userId);
    public Task<Result<Estate, ErrorMessage>> UpdateEstate(string id, EstateUpdateDTO updatedEstate);
    public Task<Result<bool, ErrorMessage>> RemoveEstate(string id);

    public Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> GetEstatesCreatedByUser(string userId,
        int page = 1, int pageSize = 10);

    public Task<Result<bool, ErrorMessage>> AddPostToEstate(string estateId, string postId);
    public Task<Result<bool, ErrorMessage>> RemovePostFromEstate(string estateId, string postId);

    public Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> SearchEstatesFilter(
        string? title = null,
        int? priceMin = null,
        int? priceMax = null,
        List<string>? categories = null,
        int skip = 0,
        int limit = 10);

    public Task<Result<bool, ErrorMessage>> AddFavoriteUserToEstate(string estateId, string userId);
    public Task<Result<bool, ErrorMessage>> RemoveFavoriteUserFromEstate(string estateId, string userId);

    public Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> GetUserFavoriteEstates(string userId,
        int page = 1, int pageSize = 10);
}