namespace NUnitTests;

[TestFixture]
public class CommentServiceTests
{
    private Mock<IMongoCollection<Comment>> _commentsCollectionMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<IPostService> _postServiceMock;
    private CommentService _commentService;

    [SetUp]
    public void SetUp()
    {
        _commentsCollectionMock = new Mock<IMongoCollection<Comment>>();
        _userServiceMock = new Mock<IUserService>();
        _postServiceMock = new Mock<IPostService>();
        _commentService = new CommentService(
            _commentsCollectionMock.Object,
            _userServiceMock.Object,
            _postServiceMock.Object
        );
    }

    #region CreateComment
    
    [Test]
    public async Task CreateComment_ShouldCreateComment_WhenDataIsValid([Values(1, 50, 1000)] int length)
    {
        var userId = "123";
        var commentDto = new CreateCommentDTO()
        {
            Content = new string('a', length),
            PostId = "456"
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

        const string generatedCommentId = "789";
        _commentsCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<Comment>(), null, It.IsAny<CancellationToken>()))
            .Callback<Comment, InsertOneOptions?, CancellationToken>((comment, _, _) =>
            {
                comment.Id = generatedCommentId;
            })
            .Returns(Task.CompletedTask);

        _postServiceMock
            .Setup(p => p.AddCommentToPost(commentDto.PostId, It.IsAny<string>()))
            .ReturnsAsync(true);

        _userServiceMock
            .Setup(u => u.AddCommentToUser(userId, It.IsAny<string>()))
            .ReturnsAsync(true);

        //Act
        (bool isError, var createdComment, ErrorMessage? error) =
            await _commentService.CreateComment(commentDto, userId);

        //Assert
        Assert.That(isError, Is.False);
        Assert.That(createdComment, Is.Not.Null);
        Assert.That(createdComment.Id, Is.EqualTo(generatedCommentId));
        Assert.That(createdComment.Content, Is.EqualTo(commentDto.Content));
        Assert.That(createdComment.Author.Id, Is.EqualTo(userId));

        _commentsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Comment>(), null, It.IsAny<CancellationToken>()), Times.Once);

        _postServiceMock.Verify(postService => postService.AddCommentToPost(commentDto.PostId, generatedCommentId),
            Times.Once);

        _userServiceMock.Verify(userService => userService.AddCommentToUser(userId, generatedCommentId),
            Times.Once);
    }
    
    [Test]
    public async Task CreateComment_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "123";
        var commentDto = new CreateCommentDTO { Content = "Valid comment", PostId = "456" };

        _userServiceMock.Setup(s => s.GetById(userId))
            .ReturnsAsync("Korisnik nije pronađen.".ToError(404));

        // Act
        (bool isError, _, ErrorMessage? error) = await _commentService.CreateComment(commentDto, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));
        
        _commentsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Comment>(), null, It.IsAny<CancellationToken>()), Times.Never);

        _postServiceMock.Verify(postService => postService.AddCommentToPost(commentDto.PostId, It.IsAny<string>()),
            Times.Never);

        _userServiceMock.Verify(userService => userService.AddCommentToUser(userId, It.IsAny<string>()),
            Times.Never);
    }
    
    [Test]
    public async Task CreateComment_ShouldFail_WhenContentIsInvalid([Values(0, 1001)] int length)
    {
        // Arrange
        var userId = "123";
        var content = new string('a', length);
        var commentDto = new CreateCommentDTO { Content = content, PostId = "456" };

        // Act
        var (isError, _, error) = await _commentService.CreateComment(commentDto, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar mora sadržati između 1 i 1000 karaktera."));
        
        _commentsCollectionMock.Verify(collection =>
            collection.InsertOneAsync(It.IsAny<Comment>(), null, It.IsAny<CancellationToken>()), Times.Never);

        _postServiceMock.Verify(postService => postService.AddCommentToPost(commentDto.PostId, It.IsAny<string>()),
            Times.Never);

        _userServiceMock.Verify(userService => userService.AddCommentToUser(userId, It.IsAny<string>()),
            Times.Never);
    }
    
    #endregion
}