using DataLayer.DTOs.Estate;
using DataLayer.DTOs.Post;

namespace NUnitTests;

[TestFixture]
public class PostServiceTests
{
    private Mock<IMongoCollection<Post>> _postsCollectionMock;
    private Mock<IAsyncCursor<Post>> _postsCursorMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<IEstateService> _estateServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private PostService _postService;

    [SetUp]
    public void Setup()
    {
        _postsCollectionMock = new Mock<IMongoCollection<Post>>();
        _postsCursorMock = new Mock<IAsyncCursor<Post>>();
        _userServiceMock = new Mock<IUserService>();
        _estateServiceMock = new Mock<IEstateService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _postService = new PostService(
            _postsCollectionMock.Object,
            _userServiceMock.Object,
            _serviceProviderMock.Object
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

    //TODO: testovi za GetAllPosts

    #endregion

    #region GetPostById

    //TODO: testovi za GetPostById

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
    public async Task UpdatePost_ShouldReturnError_WhenPostNotFound()
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
}