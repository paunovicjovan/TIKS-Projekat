namespace DataLayer.Repositories;

public class CommentAggregationRepository : ICommentAggregationRepository
{
    private readonly IMongoCollection<Comment> _commentsCollection;

    public CommentAggregationRepository(IMongoCollection<Comment> commentsCollection)
    {
        _commentsCollection = commentsCollection;
    }
    
    public async Task<List<BsonDocument>> GetCommentsForPost(string postId,
        int skip, int limit)
    {
        return await _commentsCollection.Aggregate()
            .Match(comment => comment.PostId == postId)
            .Sort(Builders<Comment>.Sort.Descending(p => p.CreatedAt))
            .Skip(skip)
            .Limit(limit)
            .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
            .As<BsonDocument>()
            .ToListAsync();
    }
}