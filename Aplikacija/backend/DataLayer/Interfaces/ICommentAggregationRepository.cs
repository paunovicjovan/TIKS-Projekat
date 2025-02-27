namespace DataLayer.Interfaces;

public interface ICommentAggregationRepository
{
    public Task<List<BsonDocument>> GetCommentsForPost(IMongoCollection<Comment> collection, string postId, int skip, int limit);
}