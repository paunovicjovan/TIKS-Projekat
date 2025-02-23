using DataLayer.DTOs.Post;

namespace NUnitTests;

[TestFixture]
public class PostServiceTests
{
    private Mock<IMongoCollection<Post>> _postsCollectionMock;
    private Mock<IAsyncCursor<Post>> _postsCursorMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<ICommentService> _commentServiceMock;
    private Mock<IEstateService> _estateServiceMock;
    private PostService _postService;

    [SetUp]
    public void Setup()
    {
        _postsCollectionMock = new Mock<IMongoCollection<Post>>();
        _postsCursorMock = new Mock<IAsyncCursor<Post>>();
        _userServiceMock = new Mock<IUserService>();
        _commentServiceMock = new Mock<ICommentService>();
        _estateServiceMock = new Mock<IEstateService>();
        _postService = new PostService(
            _postsCollectionMock.Object,
            _userServiceMock.Object,
            _estateServiceMock.Object,
            _commentServiceMock.Object
        );
    }

    #region CreatePost

    [Test]
    public async Task CreatePost_ShouldReturnPost_WhenDataIsValid()
    {
        // Arrange
        var userId = "123";
        var postDto = new CreatePostDTO()
        {
            Title = "Valid Title",
            Content = "Valid Content",
            EstateId = null
        };

        _userServiceMock
            .Setup(u => u.GetById(userId))
            .ReturnsAsync(new UserResultDTO
            {
                Id = userId,
                Username = "Petar",
                Email = "petar@gmail.com",
                PhoneNumber = "065 12 12 123"
            });

        const string generatedPostId = "789";
        _postsCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .Callback<Post, InsertOneOptions?, CancellationToken>((post, _, _) =>
            {
                post.Id = generatedPostId;
            })
            .Returns(Task.CompletedTask);

        _userServiceMock
            .Setup(u => u.AddPostToUser(userId, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        (bool isError, var createdPost, ErrorMessage? error) = await _postService.CreatePost(postDto, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost.Id, Is.EqualTo(generatedPostId));
        Assert.That(createdPost.Title, Is.EqualTo(postDto.Title));
        Assert.That(createdPost.Content, Is.EqualTo(postDto.Content));
        Assert.That(createdPost.Author.Id, Is.EqualTo(userId));

        _postsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Once);

        _userServiceMock.Verify(userService => userService.AddPostToUser(userId, generatedPostId),
            Times.Once);
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

    [Test]
    public async Task CreatePost_ShouldReturnError_WhenEstateAdditionFails()
    {
        // Arrange
        var userId = "123";
        var estateId = "estate123";
        var postDto = new CreatePostDTO { Title = "Title", Content = "Content", EstateId = estateId };

        _userServiceMock
            .Setup(u => u.GetById(userId))
            .ReturnsAsync(new UserResultDTO
            {
                Id = userId,
                Username = "Petar",
                Email = "petar@gmail.com",
                PhoneNumber = "065 12 12 123"
            });

        const string generatedPostId = "789";
        _postsCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .Callback<Post, InsertOneOptions?, CancellationToken>((post, _, _) =>
            {
                post.Id = generatedPostId;
            })
            .Returns(Task.CompletedTask);

        _userServiceMock
            .Setup(u => u.AddPostToUser(userId, It.IsAny<string>()))
            .ReturnsAsync(true);

        _estateServiceMock
            .Setup(e => e.AddPostToEstate(estateId, generatedPostId))
            .ReturnsAsync("Neuspešno dodavanje posta za nekretninu.".ToError(500));

        // Act
        (bool isError, _, ErrorMessage? error) = await _postService.CreatePost(postDto, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(500));
        Assert.That(error.Message, Is.EqualTo("Neuspešno dodavanje posta za nekretninu."));

        _postsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Once);

        _userServiceMock.Verify(userService => userService.AddPostToUser(userId, generatedPostId),
            Times.Once);

        _estateServiceMock.Verify(estateService => estateService.AddPostToEstate(estateId, generatedPostId),
            Times.Once);
    }

    #endregion
}