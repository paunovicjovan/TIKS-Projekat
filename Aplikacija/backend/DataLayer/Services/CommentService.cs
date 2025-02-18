
namespace DataLayer.Services;

public class CommentService
{
    private readonly IMongoCollection<Comment> _commentsCollection =
        DbConnection.GetDatabase().GetCollection<Comment>("comments_collection");
    
    private readonly UserService _userService;
    private readonly PostService _postService;

    public CommentService(UserService userService, PostService postService)
    {
        _userService = userService;
        _postService = postService;
    }
    
    public async Task<Result<CommentResultDTO, ErrorMessage>> CreateComment(CreateCommentDTO commentDto, string userId)
    {
        try
        {
            var newComment = new Comment
            {
                Content = commentDto.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = userId,
                PostId = commentDto.PostId
            };

            await _commentsCollection.InsertOneAsync(newComment);

            var postUpdateResult = await _postService.AddCommentToPost(commentDto.PostId, newComment.Id!);
            if (postUpdateResult.IsError)
                return postUpdateResult.Error;

            var userUpdateResult = await _userService.AddCommentToUser(userId, newComment.Id!);
            if (userUpdateResult.IsError)
                return userUpdateResult.Error;
            
            var userResult = await _userService.GetById(userId);
            if (userResult.IsError)
                return userResult.Error;
            
            var resultDto = new CommentResultDTO
            {
                Id = newComment.Id!,
                Content = newComment.Content,
                CreatedAt = newComment.CreatedAt,
                Author = userResult.Data
            };

            return resultDto;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom kreiranja komentara.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<CommentResultDTO>, ErrorMessage>> GetCommentsForPost(string postId,
        int skip = 0, int limit = 10)
    {
        try
        {
            var comments = await _commentsCollection.Aggregate()
                .Match(comment => comment.PostId == postId)
                .Sort(Builders<Comment>.Sort.Descending(p => p.CreatedAt))
                .Skip(skip)
                .Limit(limit)
                .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
                .As<BsonDocument>()
                .ToListAsync();

            var commentsDtos = comments.Select(comment => new CommentResultDTO(comment)).ToList();

            var totalCount = await _commentsCollection.CountDocumentsAsync(comment => comment.PostId == postId);

            return new PaginatedResponseDTO<CommentResultDTO>()
            {
                Data = commentsDtos,
                TotalLength = totalCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja komentara.".ToError();
        }
    }

    public async Task<Result<CommentResultDTO, ErrorMessage>> UpdateComment(string commentId,
        UpdateCommentDTO commentDto)
    {
        try
        {
            var filter = Builders<Comment>.Filter.Eq(c => c.Id, commentId);
            var update = Builders<Comment>.Update.Set(c => c.Content, commentDto.Content);

            var updateResult = await _commentsCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Komentar nije pronađen ili nije izvršena promena.".ToError();

            var updatedComment = await _commentsCollection.Find(filter).FirstOrDefaultAsync();
            if (updatedComment == null)
                return "Komentar nije pronađen.".ToError();

            var userResult = await _userService.GetById(updatedComment.AuthorId);
            if (userResult.IsError)
                return userResult.Error;

            var resultDto = new CommentResultDTO
            {
                Id = updatedComment.Id!,
                Content = updatedComment.Content,
                CreatedAt = updatedComment.CreatedAt,
                Author = userResult.Data
            };

            return resultDto;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom ažuriranja komentara.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> DeleteComment(string commentId)
    {
        try
        {
            var comment = await _commentsCollection.Find(c => c.Id == commentId).FirstOrDefaultAsync();

            if (comment == null)
                return "Komentar nije pronađen.".ToError();
            
            var filter = Builders<Comment>.Filter.Eq(c => c.Id, commentId);
            var deleteResult = await _commentsCollection.DeleteOneAsync(filter);
            
            if (deleteResult.DeletedCount <= 0)
            {
                return "Došlo je do greške prilikom brisanja komentara.".ToError();
            }
            
            var postUpdateResult = await _postService.RemoveCommentFromPost(comment.PostId, commentId);
            if (postUpdateResult.IsError)
                return postUpdateResult.Error;
                
            var userUpdateResult = await _userService.RemoveCommentFromUser(comment.AuthorId, commentId);
            if (userUpdateResult.IsError)
                return userUpdateResult.Error;

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom brisanja komentara.".ToError();
        }
    }

}