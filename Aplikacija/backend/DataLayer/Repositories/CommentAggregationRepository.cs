namespace DataLayer.Repositories;

public class CommentAggregationRepository : ICommentAggregationRepository
{
    public async Task<List<BsonDocument>> GetCommentsForPost(IMongoCollection<Comment> collection, string postId,
        int skip, int limit)
    {
        return await collection.Aggregate()
            .Match(comment => comment.PostId == postId)
            .Sort(Builders<Comment>.Sort.Descending(p => p.CreatedAt))
            .Skip(skip)
            .Limit(limit)
            .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
            .As<BsonDocument>()
            .ToListAsync();
    }
}