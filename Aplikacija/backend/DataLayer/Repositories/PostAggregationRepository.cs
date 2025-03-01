namespace DataLayer.Repositories;

public class PostAggregationRepository : IPostAggregationRepository
{
    private readonly IMongoCollection<Post> _postsCollection;

    public PostAggregationRepository(IMongoCollection<Post> postsCollection)
    {
        _postsCollection = postsCollection;
    }
    
    public Task<List<BsonDocument>> GetAllPosts(string title = "",
        int page = 1, int pageSize = 10)
    {
        return _postsCollection.Aggregate()
            .Match(post => post.Title.ToLower().Contains((title.ToLower())))
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
            .Lookup("estates_collection", "EstateId", "_id", "EstateData")
            .As<BsonDocument>()
            .ToListAsync();
    }

    public async Task<BsonDocument?> GetPostById(string id)
    {
        return await _postsCollection.Aggregate()
            .Match(Builders<Post>.Filter.Eq("_id", ObjectId.Parse(id)))
            .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
            .Lookup("estates_collection", "EstateId", "_id", "EstateData")
            .As<BsonDocument>()
            .FirstOrDefaultAsync();
    }
}