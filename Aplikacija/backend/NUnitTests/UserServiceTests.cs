namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IMongoCollection<User>> _usersCollectionMock;
    private Mock<IAsyncCursor<User>> _usersCursorMock;
    private Mock<ITokenService> _tokenServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;

    [SetUp]
    public void SetUp()
    {
        _usersCollectionMock = new Mock<IMongoCollection<User>>();
        _usersCursorMock = new Mock<IAsyncCursor<User>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    [Test]
    //imenovanje: NazivMetodeKojaSeTestira_OcekivaniIshod_Uslov
    public async Task Register_RegistersUser_WhenUserIsValid()
    {
        // Arrange
        const string generatedId = "some-id";
        _usersCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
            .Callback<User, InsertOneOptions?, CancellationToken>((user, _, _) => { user.Id = generatedId; })
            .Returns(Task.CompletedTask);

        _usersCursorMock
            .Setup(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);

        _usersCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<User>());

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        const string fakeJwt = "some-jwt";
        _tokenServiceMock.Setup(service => service.CreateToken(It.IsAny<User>()))
            .Returns(fakeJwt);

        var userService =
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object);

        var userDto = new CreateUserDTO()
        {
            Username = "Petar",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        // Act
        (bool isError, var result, ErrorMessage? error) = await userService.Register(userDto);

        // Assert
        // uvek koristimo Assert.That metodu
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(generatedId));
        Assert.That(result.Username, Is.EqualTo(userDto.Username));
        Assert.That(result.Email, Is.EqualTo(userDto.Email));
        Assert.That(result.PhoneNumber, Is.EqualTo(userDto.PhoneNumber));
        Assert.That(result.Role, Is.EqualTo(UserRole.User));
        Assert.That(result.Token, Is.EqualTo(fakeJwt));

        _usersCollectionMock.Verify(
            collection => collection.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()),
            times: Times.Once);

        _tokenServiceMock.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task Register_ReturnsError_WhenUsernameIsNotValid()
    {
        // Arrange
        const string generatedId = "some-id";
        _usersCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
            .Callback<User, InsertOneOptions?, CancellationToken>((user, _, _) => { user.Id = generatedId; })
            .Returns(Task.CompletedTask);

        _usersCursorMock
            .Setup(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);

        _usersCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<User>());

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var userService =
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object);

        var userDto = new CreateUserDTO()
        {
            Username = "some invalid ? #username",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        // Act
        (bool isError, var result, ErrorMessage? error) = await userService.Register(userDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message,
            Is.EqualTo(
                "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i ."));

        _usersCollectionMock.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(), It.IsAny<CancellationToken>()), Times.Never);

        _usersCollectionMock.Verify(c => c.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Test]
    [Ignore("Ne radi jos")]
    public async Task Register_ReturnsError_WhenUsernameIsTaken()
    {
        // Arrange
        const string generatedId = "some-id";
        _usersCollectionMock
            .Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
            .Callback<User, InsertOneOptions?, CancellationToken>((user, _, _) => { user.Id = generatedId; })
            .Returns(Task.CompletedTask);

        var userDto = new CreateUserDTO()
        {
            Username = "Petar",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        var existingUser = new User()
        {
            Id = "123",
            Username = userDto.Username,
            Email = userDto.Email,
            PasswordHash = "123",
            PhoneNumber = "066 123 12 12"
        };
        
        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _usersCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var userService =
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object);

        // Act
        (bool isError, var result, ErrorMessage? error) = await userService.Register(userDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Već postoji korisnik sa unetim korisničkim imenom."));

        _usersCollectionMock.Verify(c => c.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }
}