using Microsoft.Extensions.DependencyInjection;

namespace DataLayer.Services;

public class PostService
{
    private readonly IMongoCollection<Post> _postsCollection =
        DbConnection.GetDatabase().GetCollection<Post>("posts_collection");

    private readonly UserService _userService;
    private readonly IServiceProvider _serviceProvider;

    public PostService(UserService userService, IServiceProvider serviceProvider)
    {
        _userService = userService;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<PostResultDTO, ErrorMessage>> CreatePost(CreatePostDTO postDto, string userId)
    {
        try
        {
            var newPost = new Post
            {
                Title = postDto.Title,
                Content = postDto.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = userId,
                CommentIds = [],
                EstateId = postDto.EstateId
            };

            await _postsCollection.InsertOneAsync(newPost);

            var userUpdateResult = await _userService.AddPostToUser(userId, newPost.Id!);
            if (userUpdateResult.IsError)
                return userUpdateResult.Error;

            var userResult = await _userService.GetById(userId);
            if (userResult.IsError)
                return userResult.Error;

            EstateResultDTO? estate = null;
            if (postDto.EstateId != null)
            {
                EstateService estateService = _serviceProvider.GetRequiredService<EstateService>();
                var estateUpdateResult = await estateService.AddPostToEstate(postDto.EstateId, newPost.Id!);
                if (estateUpdateResult.IsError)
                    return estateUpdateResult.Error;

                var estateResult = await estateService.GetEstate(postDto.EstateId);
                if (estateResult.IsError)
                    return estateResult.Error;

                estate = estateResult.Data;
            }

            var resultDto = new PostResultDTO
            {
                Id = newPost.Id!,
                Title = newPost.Title,
                Content = newPost.Content,
                CreatedAt = newPost.CreatedAt,
                Author = userResult.Data,
                Estate = estate
            };

            return resultDto;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom kreiranja objave.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetAllPosts(string title = "",
        int page = 1, int pageSize = 10)
    {
        try
        {
            var posts = await _postsCollection.Aggregate()
                .Match(post => post.Title.ToLower().Contains((title.ToLower())))
                .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
                .Lookup("estates_collection", "EstateId", "_id", "EstateData")
                .As<BsonDocument>()
                .ToListAsync();


            var postsDtos = posts.Select(post => new PostResultDTO(post)).ToList();

            var totalCount =
                await _postsCollection.CountDocumentsAsync(post => post.Title.ToLower().Contains((title.ToLower())));

            return new PaginatedResponseDTO<PostResultDTO>()
            {
                Data = postsDtos,
                TotalLength = totalCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja objava.".ToError();
        }
    }

    public async Task<Result<PostResultDTO, ErrorMessage>> GetPostById(string postId)
    {
        try
        {
            var post = await _postsCollection.Aggregate()
                .Match(Builders<Post>.Filter.Eq("_id", ObjectId.Parse(postId)))
                .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
                .Lookup("estates_collection", "EstateId", "_id", "EstateData")
                .As<BsonDocument>()
                .FirstOrDefaultAsync();

            if (post == null)
                return "Post nije pronađen.".ToError(404);

            var postDto = new PostResultDTO(post);

            return postDto;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja objave.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetAllPostsForEstate(string estateId,
        int page = 1, int pageSize = 10)
    {
        try
        {
            var posts = await _postsCollection.Aggregate()
                .Match(post => post.EstateId == estateId)
                .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
                .As<BsonDocument>()
                .ToListAsync();

            var postsDtos = posts.Select(post => new PostResultDTO(post)).ToList();

            var totalCount = await _postsCollection.CountDocumentsAsync(post => post.EstateId == estateId);

            return new PaginatedResponseDTO<PostResultDTO>()
            {
                Data = postsDtos,
                TotalLength = totalCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja objava.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> UpdatePost(string postId, UpdatePostDTO postDto)
    {
        try
        {
            var existingPost = await _postsCollection
                .Find(p => p.Id == postId)
                .FirstOrDefaultAsync();

            if (existingPost == null)
            {
                return "Objava sa datim ID-jem ne postoji.".ToError(404);
            }

            existingPost.Title = postDto.Title;
            existingPost.Content = postDto.Content;

            var res = await _postsCollection.ReplaceOneAsync(p => p.Id == postId, existingPost);

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom ažuriranja objave.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> DeletePost(string postId)
    {
        try
        {
            var existingPost = await _postsCollection
                .Find(p => p.Id == postId)
                .FirstOrDefaultAsync();

            if (existingPost == null)
            {
                return "Objava sa datim ID-jem ne postoji.".ToError();
            }

            var userUpdateResult = await _userService.RemovePostFromUser(existingPost.AuthorId, postId);
            if (userUpdateResult.IsError)
                return userUpdateResult.Error;

            if (existingPost.EstateId != null)
            {
                EstateService estateService = _serviceProvider.GetRequiredService<EstateService>();
                var estateUpdateResult = await estateService.RemovePostFromEstate(existingPost.EstateId, postId);
                if (estateUpdateResult.IsError)
                    return estateUpdateResult.Error;
            }

            var commentService = _serviceProvider.GetRequiredService<CommentService>();

            foreach (var commentId in existingPost.CommentIds)
            {
                await commentService.DeleteComment(commentId);
            }

            var deleteResult = await _postsCollection.DeleteOneAsync(p => p.Id == postId);

            if (deleteResult.DeletedCount == 0)
            {
                return "Došlo je do greške prilikom brisanja objave.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom brisanja objave.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddCommentToPost(string postId, string commentId)
    {
        try
        {
            var updateResult = await _postsCollection.UpdateOneAsync(
                p => p.Id == postId,
                Builders<Post>.Update.Push(p => p.CommentIds, commentId)
            );

            if (updateResult.ModifiedCount == 0)
            {
                return "Objava nije pronađena ili nije ažurirana.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja komentara objavi.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemoveCommentFromPost(string postId, string commentId)
    {
        try
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update.Pull(p => p.CommentIds, commentId);

            var updateResult = await _postsCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Komentar nije pronađen na objavi.".ToError();

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja komentara sa objave.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<PostResultDTO>, ErrorMessage>> GetUserPosts(string userId, int page = 1, int pageSize = 10)
    {
        try
        {
            var posts = await _postsCollection.Aggregate()
                .Match(p => p.AuthorId == userId)
                .SortByDescending(p => p.CreatedAt)
                .Skip((page-1)*pageSize)
                .Limit(pageSize)
                .Lookup("users_collection", "AuthorId", "_id", "AuthorData")
                .Lookup("estates_collection", "EstateId", "_id", "EstateData")
                .As<BsonDocument>()
                .ToListAsync();

            var postDtos = posts.Select(post => new PostResultDTO(post)).ToList();
            
            var totalCount = await _postsCollection.CountDocumentsAsync(p => p.AuthorId == userId);

            return new PaginatedResponseDTO<PostResultDTO>
            {
                Data = postDtos,
                TotalLength = totalCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja objava.".ToError();
        }
    }
}