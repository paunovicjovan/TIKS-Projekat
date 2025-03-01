namespace DataLayer.Interfaces;

public interface IPostAggregationRepository
{
    public Task<List<BsonDocument>> GetAllPosts(string title = "", int page = 1, int pageSize = 10);
    public Task<BsonDocument?> GetPostById(string id);
}