using DataLayer.DTOs.Estate;
using DataLayer.DTOs.Post;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace NUnitTests;

[TestFixture]
public class PostServiceTests
{
    private Mock<IMongoCollection<Post>> _postsCollectionMock;
    private Mock<IAsyncCursor<Post>> _postsCursorMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<IEstateService> _estateServiceMock;
    private Mock<ICommentService> _commentServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPostAggregationRepository> _postAggregationRepositoryMock;
    private PostService _postService;

    [SetUp]
    public void Setup()
    {
        _postsCollectionMock = new Mock<IMongoCollection<Post>>();
        _postsCursorMock = new Mock<IAsyncCursor<Post>>();
        _userServiceMock = new Mock<IUserService>();
        _estateServiceMock = new Mock<IEstateService>();
        _commentServiceMock = new Mock<ICommentService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _postAggregationRepositoryMock = new Mock<IPostAggregationRepository>();
        _postService = new PostService(
            _postsCollectionMock.Object,
            _userServiceMock.Object,
            _serviceProviderMock.Object,
            _postAggregationRepositoryMock.Object
        );
    }

    #region CreatePost

    [Test]
    public async Task CreatePost_ShouldReturnPost_WhenCreatingPostWithEstate()
    {
        // Arrange
        var userId = "123";
        var estateId = "456";
        var postDto = new CreatePostDTO()
        {
            Title = "Valid Title",
            Content = "Valid Content",
            EstateId = estateId
        };

        const string generatedPostId = "789";
        _postsCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .Callback<Post, InsertOneOptions?, CancellationToken>((post, _, _) => { post.Id = generatedPostId; })
            .Returns(Task.CompletedTask);

        _userServiceMock
            .Setup(u => u.AddPostToUser(userId, It.IsAny<string>()))
            .ReturnsAsync(true);

        _userServiceMock
            .Setup(u => u.GetById(userId))
            .ReturnsAsync(new UserResultDTO
            {
                Id = userId,
                Username = "Petar",
                Email = "petar@gmail.com",
                PhoneNumber = "065 12 12 123"
            });

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock.Setup(service => service.AddPostToEstate(It.IsAny<string>(), generatedPostId))
            .ReturnsAsync(true);

        var estateResult = new EstateResultDTO()
        {
            Id = estateId,
            Title = "Naslov",
            Description = "Opis",
            Price = 100000,
            SquareMeters = 75,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Images = ["image1.jpg", "image2.jpg"],
            Longitude = 19.8335,
            Latitude = 45.2671
        };

        _estateServiceMock.Setup(service => service.GetEstate(It.IsAny<string>()))
            .ReturnsAsync(estateResult);

        // Act
        (bool isError, var createdPost, ErrorMessage? error) = await _postService.CreatePost(postDto, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost.Id, Is.EqualTo(generatedPostId));
        Assert.That(createdPost.Title, Is.EqualTo(postDto.Title));
        Assert.That(createdPost.Content, Is.EqualTo(postDto.Content));
        Assert.That(createdPost.Author.Id, Is.EqualTo(userId));
        Assert.That(createdPost.Estate, Is.Not.Null);
        Assert.That(createdPost.Estate.Id, Is.EqualTo(estateId));

        _postsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Once);

        _userServiceMock.Verify(userService => userService.AddPostToUser(userId, generatedPostId),
            Times.Once);

        _estateServiceMock.Verify(service => service.AddPostToEstate(estateResult.Id, generatedPostId), Times.Once);
    }

    [Test]
    public async Task CreatePost_ShouldReturnPost_WhenCreatingPostWithoutEstate()
    {
        // Arrange
        var userId = "123";
        var postDto = new CreatePostDTO()
        {
            Title = "Valid Title",
            Content = "Valid Content",
            EstateId = null
        };

        const string generatedPostId = "789";
        _postsCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .Callback<Post, InsertOneOptions?, CancellationToken>((post, _, _) => { post.Id = generatedPostId; })
            .Returns(Task.CompletedTask);

        _userServiceMock
            .Setup(u => u.AddPostToUser(userId, It.IsAny<string>()))
            .ReturnsAsync(true);

        _userServiceMock
            .Setup(u => u.GetById(userId))
            .ReturnsAsync(new UserResultDTO
            {
                Id = userId,
                Username = "Petar",
                Email = "petar@gmail.com",
                PhoneNumber = "065 12 12 123"
            });

        // Act
        (bool isError, var createdPost, ErrorMessage? error) = await _postService.CreatePost(postDto, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost.Id, Is.EqualTo(generatedPostId));
        Assert.That(createdPost.Title, Is.EqualTo(postDto.Title));
        Assert.That(createdPost.Content, Is.EqualTo(postDto.Content));
        Assert.That(createdPost.Author.Id, Is.EqualTo(userId));
        Assert.That(createdPost.Estate, Is.Null);

        _postsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Once);

        _userServiceMock.Verify(userService => userService.AddPostToUser(userId, generatedPostId),
            Times.Once);

        _estateServiceMock.Verify(estateService => estateService.AddPostToEstate(It.IsAny<string>(), generatedPostId),
            Times.Never);
    }

    [Test]
    public async Task CreatePost_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "123";
        var postDto = new CreatePostDTO { Title = "Title", Content = "Content", EstateId = null };

        _userServiceMock.Setup(s => s.GetById(userId))
            .ReturnsAsync("Korisnik nije pronađen.".ToError(404));

        // Act
        (bool isError, _, ErrorMessage? error) = await _postService.CreatePost(postDto, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));

        _postsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Never);

        _userServiceMock.Verify(userService => userService.AddPostToUser(userId, It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region GetAllPosts

    [Test]
    [TestCase(1, 2, 2, 5)]
    [TestCase(2, 2, 2, 5)]
    [TestCase(3, 2, 1, 5)]
    [TestCase(4, 2, 0, 5)]
    public async Task GetAllPosts_ShouldReturnCorrectPaginatedPosts_WhenParamsAreValid(int page, int pageSize,
        int expectedCount, int totalPostsCount)
    {
        // Arrange
        var mockPosts = new List<BsonDocument>();
        for (int i = 0; i < totalPostsCount; i++)
        {
            mockPosts.Add(new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "Title", $"Post {i + 1}" },
                { "Content", $"Content {i + 1}" },
                { "CreatedAt", DateTime.UtcNow },
                {
                    "AuthorData",
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            { "_id", ObjectId.GenerateNewId() },
                            { "Username", $"User{i + 1}" },
                            { "Email", $"user{i + 1}@example.com" },
                            { "PhoneNumber", $"broj {i}" },
                            { "Role", UserRole.Admin }
                        }
                    }
                }
            });
        }

        _postAggregationRepositoryMock
            .Setup(repo => repo.GetAllPosts(It.IsAny<string>(), page, pageSize))
            .ReturnsAsync(mockPosts.Skip((page - 1) * pageSize).Take(pageSize).ToList());

        _postsCollectionMock
            .Setup(col => col.CountDocumentsAsync(It.IsAny<FilterDefinition<Post>>(), null, default))
            .ReturnsAsync(totalPostsCount);

        // Act
        (bool isError, var paginatedPosts, ErrorMessage? error) = await _postService.GetAllPosts("", page, pageSize);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(paginatedPosts, Is.Not.Null);
        Assert.That(paginatedPosts.Data, Is.Not.Null);
        Assert.That(paginatedPosts.Data.Count, Is.EqualTo(expectedCount));
        Assert.That(paginatedPosts.TotalLength, Is.EqualTo(totalPostsCount));
    }

    [Test]
    public async Task GetAllPosts_ShouldReturnEmptyList_WhenNoPostsExist()
    {
        // Arrange
        _postAggregationRepositoryMock
            .Setup(repo => repo.GetAllPosts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        _postsCollectionMock
            .Setup(col => col.CountDocumentsAsync(It.IsAny<FilterDefinition<Post>>(), null, default))
            .ReturnsAsync(0);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.GetAllPosts();

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.TotalLength, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllPosts_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _postAggregationRepositoryMock
            .Setup(repo => repo.GetAllPosts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.GetAllPosts();

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objava."));
    }

    #endregion

    #region GetPostById

    [Test]
    public async Task GetPostById_ShouldReturnPost_WhenPostExists()
    {
        // Arrange
        var postId = ObjectId.GenerateNewId().ToString();
        var post = new BsonDocument
        {
            { "_id", new ObjectId(postId) },
            { "Title", "Naslov" },
            { "Content", "Sadrzaj" },
            { "CreatedAt", DateTime.UtcNow },
            {
                "AuthorData", new BsonArray
                {
                    new BsonDocument
                    {
                        { "_id", new ObjectId() },
                        { "Username", "author123" },
                        { "Email", "author@example.com" },
                        { "PhoneNumber", "1234567890" },
                        { "Role", 1 }
                    }
                }
            },
        };

        _postAggregationRepositoryMock.Setup(repo => repo.GetPostById(postId)).ReturnsAsync(post);

        // Act
        (bool isError, var postDto, ErrorMessage? error) = await _postService.GetPostById(postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(postDto, Is.Not.Null & Is.InstanceOf<PostResultDTO>());
        Assert.That(postDto.Title, Is.EqualTo(post["Title"].AsString));
        Assert.That(postDto.Content, Is.EqualTo(post["Content"].AsString));
        Assert.That(postDto.Author.Username, Is.EqualTo(post["AuthorData"].AsBsonArray.First()["Username"].AsString));
    }

    [Test]
    public async Task GetPostById_ShouldReturnError_WhenPostDoesNotExist()
    {
        // Arrange
        var postId = ObjectId.GenerateNewId().ToString();
        _postAggregationRepositoryMock.Setup(repo => repo.GetPostById(postId))
            .ReturnsAsync((BsonDocument?)null);

        // Act
        (bool isError, var postDto, ErrorMessage? error) = await _postService.GetPostById(postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Post nije pronađen."));
    }
    
    [Test]
    public async Task GetPostById_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var postId = ObjectId.GenerateNewId().ToString();
        _postAggregationRepositoryMock.Setup(repo => repo.GetPostById(postId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var postDto, ErrorMessage? error) = await _postService.GetPostById(postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objave."));
    }


    #endregion

    #region GetAllPostsForEstate

    //TODO: testovi za GetAllPostsForEstate

    #endregion

    #region UpdatePost

    [Test]
    public async Task UpdatePost_ShouldReturnTrue_WhenUpdateIsSuccessful()
    {
        // Arrange
        var postId = "123";

        var postDto = new UpdatePostDTO
        {
            Title = "Updated Title",
            Content = "Updated Content"
        };

        var existingPost = new Post
        {
            Id = postId,
            Title = "Old Title",
            Content = "Old Content",
            AuthorId = "456"
        };

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCursorMock.Setup(x => x.Current).Returns(new List<Post> { existingPost });

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        _postsCollectionMock
            .Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<Post>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.UpdatePost(postId, postDto);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);

        _postsCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Post>>(),
            It.IsAny<Post>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdatePost_ShouldReturnError_WhenPostDoesNotExist()
    {
        // Arrange
        var postId = "123";
        var postDto = new UpdatePostDTO
        {
            Title = "Updated Title",
            Content = "Updated Content"
        };

        _postsCursorMock.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.UpdatePost(postId, postDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Objava sa datim ID-jem ne postoji."));

        _postsCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Post>>(),
            It.IsAny<Post>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdatePost_ShouldReturnFalse_WhenReplaceFails()
    {
        // Arrange
        var postId = "123";
        var postDto = new UpdatePostDTO { Title = "Updated Title", Content = "Updated Content" };
        var existingPost = new Post
        {
            Id = postId,
            Title = "Old Title",
            Content = "Old Content",
            AuthorId = "456"
        };

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCursorMock.Setup(x => x.Current).Returns(new List<Post> { existingPost });

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        _postsCollectionMock
            .Setup(collection => collection.ReplaceOneAsync(It.IsAny<FilterDefinition<Post>>(), It.IsAny<Post>(),
                It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var result, var error) = await _postService.UpdatePost(postId, postDto);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.False);
        Assert.That(error, Is.Null);

        _postsCollectionMock.Verify(collection => collection.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<Post>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeletePost

    [Test]
    public async Task DeletePost_ShouldReturnTrue_WhenDeletionIsSuccessful()
    {
        // Arrange
        var postId = "123";
        var existingPost = new Post
        {
            Id = postId,
            Title = "Post",
            Content = "Content",
            AuthorId = "456",
            EstateId = "789",
            CommentIds = new List<string> { "c1", "c2" }
        };

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCursorMock.Setup(x => x.Current).Returns(new List<Post> { existingPost });

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        _userServiceMock
            .Setup(x => x.RemovePostFromUser(existingPost.AuthorId, postId))
            .ReturnsAsync(true);

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock
            .Setup(x => x.RemovePostFromEstate(existingPost.EstateId, postId))
            .ReturnsAsync(true);

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(ICommentService)))
            .Returns(_commentServiceMock.Object);

        _commentServiceMock
            .Setup(x => x.DeleteComment(It.IsAny<string>()))
            .ReturnsAsync(true);

        _postsCollectionMock
            .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.DeletePost(postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);

        _userServiceMock.Verify(x => x.RemovePostFromUser(existingPost.AuthorId, postId), Times.Once);
        _estateServiceMock.Verify(x => x.RemovePostFromEstate(existingPost.EstateId, postId), Times.Once);
        _postsCollectionMock.Verify(
            x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Post>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeletePost_ShouldReturnError_WhenPostDoesNotExist()
    {
        // Arrange
        var postId = "123";

        _postsCursorMock.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.DeletePost(postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Objava sa datim ID-jem ne postoji."));

        _userServiceMock.Verify(x => x.RemovePostFromUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _estateServiceMock.Verify(x => x.RemovePostFromEstate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _commentServiceMock.Verify(x => x.DeleteComment(It.IsAny<string>()), Times.Never);
        _postsCollectionMock.Verify(
            x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Post>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeletePost_ShouldReturnError_WhenDeletionFails()
    {
        // Arrange
        var postId = "123";
        var existingPost = new Post
        {
            Id = postId,
            Title = "Post",
            Content = "Content",
            AuthorId = "456",
            EstateId = "789",
            CommentIds = new List<string> { "c1", "c2" }
        };

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCursorMock.Setup(x => x.Current).Returns(new List<Post> { existingPost });

        _postsCollectionMock
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<FindOptions<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_postsCursorMock.Object);

        _userServiceMock
            .Setup(x => x.RemovePostFromUser(existingPost.AuthorId, postId))
            .ReturnsAsync(true);

        _estateServiceMock
            .Setup(x => x.RemovePostFromEstate(existingPost.EstateId, postId))
            .ReturnsAsync(true);

        _commentServiceMock
            .Setup(x => x.DeleteComment(It.IsAny<string>()))
            .ReturnsAsync(true);

        _postsCollectionMock
            .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        (bool isError, _, var error) = await _postService.DeletePost(postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom brisanja objave."));
    }

    #endregion

    #region AddCommentToPost

    [Test]
    public async Task AddCommentToPost_ShouldReturnTrue_WhenCommentIsAddedSuccessfully()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.AddCommentToPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AddCommentToPost_ShouldReturnError_WhenPostNotFound()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.AddCommentToPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Objava nije pronađena ili nije ažurirana."));

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AddCommentToPost_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.AddCommentToPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom dodavanja komentara objavi."));

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RemoveCommentFromPost

    [Test]
    public async Task RemoveCommentFromPost_ShouldReturnTrue_WhenCommentIsRemovedSuccessfully()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.RemoveCommentFromPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveCommentFromPost_ShouldReturnError_WhenCommentNotFoundOnPost()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.RemoveCommentFromPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar nije pronađen na objavi."));

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveCommentFromPost_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var postId = "123";
        var commentId = "456";

        _postsCursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _postsCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<Post>>(),
                It.IsAny<UpdateDefinition<Post>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _postService.RemoveCommentFromPost(postId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom uklanjanja komentara sa objave."));

        _postsCollectionMock.Verify(collection =>
                collection.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Post>>(),
                    It.IsAny<UpdateDefinition<Post>>(),
                    null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetUserPosts

    //TODO: testovi za GetUserPosts

    #endregion
}