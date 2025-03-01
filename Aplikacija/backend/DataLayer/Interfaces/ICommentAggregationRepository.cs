namespace DataLayer.Interfaces;

public interface ICommentAggregationRepository
{
    public Task<List<BsonDocument>> GetCommentsForPost(string postId, int skip, int limit);
}