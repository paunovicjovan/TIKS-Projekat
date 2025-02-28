using DataLayer.DTOs.Pagination;

namespace NUnitTests;

[TestFixture]
public class CommentServiceTests
{
    private Mock<IMongoCollection<Comment>> _commentsCollectionMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<IPostService> _postServiceMock;
    private Mock<IAsyncCursor<Comment>> _commentsCursorMock;
    private Mock<ICommentAggregationRepository> _commentAggregationRepositoryMock;
    private CommentService _commentService;

    [SetUp]
    public void SetUp()
    {
        _commentsCollectionMock = new Mock<IMongoCollection<Comment>>();
        _userServiceMock = new Mock<IUserService>();
        _postServiceMock = new Mock<IPostService>();
        _commentsCursorMock = new Mock<IAsyncCursor<Comment>>();
        _commentAggregationRepositoryMock = new Mock<ICommentAggregationRepository>();
        _commentService = new CommentService(
            _commentsCollectionMock.Object,
            _userServiceMock.Object,
            _postServiceMock.Object,
            _commentAggregationRepositoryMock.Object
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
    public async Task CreateComment_ShouldReturnError_WhenContentIsInvalid([Values(0, 1001)] int length)
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

    #region GetCommentsForPost

    [Test]
    [TestCase(0, 2, 2)]
    [TestCase(1, 2, 2)]
    [TestCase(2, 2, 1)]
    [TestCase(3, 2, 0)]
    public async Task GetCommentsForPost_ShouldReturnCorrectPaginatedComments(int skip, int limit, int expectedCount)
    {
        // Arrange
        const string postId = "123";

        var author = new BsonDocument
        {
            { "_id", new ObjectId("507f191e810c19729de860ea") },
            { "Username", "Petar" },
            { "Email", "petar@gmail.com" },
            { "PhoneNumber", "062 1212 123" },
            { "Role", (int)UserRole.User }
        };

        var mockComments = new List<BsonDocument>
        {
            new BsonDocument
            {
                { "_id", new ObjectId("5f2a1e730f3f9c3a9e6c8742") },
                { "Content", "Komentar 1" },
                { "CreatedAt", DateTime.Now },
                { "AuthorData", new BsonArray { author } }
            },
            new BsonDocument
            {
                { "_id", new ObjectId("60d5f06a5e3a5fbb31798765") },
                { "Content", "Komentar 2" },
                { "CreatedAt", DateTime.Now },
                { "AuthorData", new BsonArray { author } }
            },
            new BsonDocument
            {
                { "_id", new ObjectId("60d5f06a5e3a5fbb31798766") },
                { "Content", "Komentar 3" },
                { "CreatedAt", DateTime.Now },
                { "AuthorData", new BsonArray { author } }
            }
        };

        var totalCount = mockComments.Count;

        _commentAggregationRepositoryMock.Setup(repo =>
                repo.GetCommentsForPost(It.IsAny<IMongoCollection<Comment>>(), postId, skip, limit))
            .ReturnsAsync(mockComments.Skip(skip).Take(limit).ToList());

        _commentsCollectionMock
            .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Comment>>(), It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);

        // Act
        (bool isError, var paginatedComments, ErrorMessage? error) =
            await _commentService.GetCommentsForPost(postId, skip, limit);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(paginatedComments, Is.Not.Null);
        Assert.That(paginatedComments.Data, Is.Not.Null);
        Assert.That(paginatedComments.Data.Count, Is.EqualTo(expectedCount));
        Assert.That(paginatedComments.TotalLength, Is.EqualTo(totalCount));
    }

    [Test]
    public async Task GetCommentsForPost_ShouldReturnEmptyList_WhenNoCommentsExist()
    {
        // Arrange
        string postId = "123";
        _commentAggregationRepositoryMock
            .Setup(repo =>
                repo.GetCommentsForPost(_commentsCollectionMock.Object, postId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        _commentsCollectionMock
            .Setup(col => col.CountDocumentsAsync(It.IsAny<FilterDefinition<Comment>>(), null, default))
            .ReturnsAsync(0);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _commentService.GetCommentsForPost(postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.TotalLength, Is.EqualTo(0));
    }

    [Test]
    public async Task GetCommentsForPost_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        string postId = "123";
        _commentAggregationRepositoryMock
            .Setup(repo =>
                repo.GetCommentsForPost(_commentsCollectionMock.Object, postId, It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _commentService.GetCommentsForPost(postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja komentara."));
    }

    #endregion

    #region UpdateComment

    [Test]
    public async Task UpdateComment_ShouldReturnUpdatedComment_WhenSuccessful()
    {
        // Arrange
        const string commentId = "123";
        var commentDto = new UpdateCommentDTO { Content = "Novi sadrzaj" };

        const string authorId = "456";
        var updatedComment = new Comment
        {
            Id = commentId,
            Content = commentDto.Content,
            AuthorId = authorId,
            PostId = "789",
            CreatedAt = DateTime.Now
        };

        _commentsCollectionMock
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<UpdateDefinition<Comment>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        _commentsCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _commentsCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _commentsCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<Comment>() { updatedComment });

        _commentsCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<FindOptions<Comment, Comment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_commentsCursorMock.Object);

        var author = new UserResultDTO
        {
            Id = authorId,
            Username = "Petar",
            Email = "petar@gmail.com",
            PhoneNumber = "062 1212 123"
        };

        _userServiceMock
            .Setup(u => u.GetById(It.IsAny<string>()))
            .ReturnsAsync(author);

        // Act
        (bool isError, var updatedCommentResult, ErrorMessage? error) =
            await _commentService.UpdateComment(commentId, commentDto);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(updatedCommentResult, Is.Not.Null);
        Assert.That(updatedCommentResult.Id, Is.EqualTo(commentId));
        Assert.That(updatedCommentResult.Content, Is.EqualTo(commentDto.Content));
        Assert.That(updatedCommentResult.Author.Id, Is.EqualTo(author.Id));

        _commentsCollectionMock.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<UpdateDefinition<Comment>>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userServiceMock.Verify(service => service.GetById(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task UpdateComment_ShouldReturnError_WhenContentIsInvalid([Values(0, 1001)] int contentLength)
    {
        // Arrange
        const string commentId = "123";
        var commentDto = new UpdateCommentDTO
        {
            Content = new string('a', contentLength)
        };

        // Act
        (bool isError, _, ErrorMessage? error) = await _commentService.UpdateComment(commentId, commentDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar mora sadržati između 1 i 1000 karaktera."));

        _commentsCollectionMock.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<UpdateDefinition<Comment>>(),
                null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateComment_ShouldReturnError_WhenCommentNotFoundOrNoChangesAreMade()
    {
        // Arrange
        const string commentId = "123";
        var commentDto = new UpdateCommentDTO { Content = "Novi sadrzaj" };

        _commentsCollectionMock
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<UpdateDefinition<Comment>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, _, ErrorMessage? error) = await _commentService.UpdateComment(commentId, commentDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar nije pronađen ili nije izvršena promena."));

        _commentsCollectionMock.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<UpdateDefinition<Comment>>(),
                null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteComment

    [Test]
    public async Task DeleteComment_ShouldReturnTrue_WhenCommentIsDeletedSuccessfully()
    {
        // Arrange
        var existingComment = new Comment()
        {
            Id = "123",
            PostId = "456",
            AuthorId = "789",
            Content = "Ovo je komentar"
        };

        _commentsCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _commentsCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _commentsCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<Comment> { existingComment });

        _commentsCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<FindOptions<Comment, Comment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_commentsCursorMock.Object);

        _commentsCollectionMock
            .Setup(collection =>
                collection.DeleteOneAsync(It.IsAny<FilterDefinition<Comment>>(), CancellationToken.None))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        _postServiceMock
            .Setup(service => service.RemoveCommentFromPost(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _userServiceMock
            .Setup(service => service.RemoveCommentFromUser(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _commentService.DeleteComment("123");

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);
        Assert.That(error, Is.Null);

        _commentsCollectionMock.Verify(
            collection => collection.DeleteOneAsync(It.IsAny<FilterDefinition<Comment>>(), CancellationToken.None),
            Times.Once);
        _postServiceMock.Verify(service => service.RemoveCommentFromPost(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        _userServiceMock.Verify(service => service.RemoveCommentFromUser(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteComment_ShouldReturnError_WhenCommentNotFound()
    {
        // Arrange
        var nonExistingCommentId = "999";

        _commentsCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<FindOptions<Comment, Comment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncCursor<Comment>>());

        // Act
        (bool isError, var _, ErrorMessage? error) = await _commentService.DeleteComment(nonExistingCommentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar nije pronađen."));

        _commentsCollectionMock.Verify(
            collection => collection.DeleteOneAsync(It.IsAny<FilterDefinition<Comment>>(), CancellationToken.None),
            Times.Never);
        _postServiceMock.Verify(service => service.RemoveCommentFromPost(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _userServiceMock.Verify(service => service.RemoveCommentFromUser(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteComment_ShouldReturnError_WhenDeleteFails()
    {
        // Arrange
        var existingComment = new Comment()
        {
            Id = "123",
            PostId = "456",
            AuthorId = "789",
            Content = "Ovo je komentar"
        };

        _commentsCursorMock
            .Setup(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _commentsCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<Comment> { existingComment });

        _commentsCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<Comment>>(),
                It.IsAny<FindOptions<Comment, Comment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_commentsCursorMock.Object);

        _commentsCollectionMock
            .Setup(collection =>
                collection.DeleteOneAsync(It.IsAny<FilterDefinition<Comment>>(), CancellationToken.None))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        (bool isError, var _, ErrorMessage? error) = await _commentService.DeleteComment("123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom brisanja komentara."));

        _commentsCollectionMock.Verify(
            collection => collection.DeleteOneAsync(It.IsAny<FilterDefinition<Comment>>(), CancellationToken.None),
            Times.Once);
        _postServiceMock.Verify(service => service.RemoveCommentFromPost(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _userServiceMock.Verify(service => service.RemoveCommentFromUser(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion
}