using System.Security.Claims;

namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IMongoCollection<User>> _usersCollectionMock;
    private Mock<IAsyncCursor<User>> _usersCursorMock;
    private Mock<ITokenService> _tokenServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPasswordHasher<User>> _passwordHasherMock;

    [SetUp]
    public void SetUp()
    {
        _usersCollectionMock = new Mock<IMongoCollection<User>>();
        _usersCursorMock = new Mock<IAsyncCursor<User>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
    }

    #region Register

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

        var userDto = new CreateUserDTO()
        {
            Username = "Petar",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        const string fakePasswordHash = "password-hash";
        _passwordHasherMock.Setup(hasher => hasher.HashPassword(It.IsAny<User>(), userDto.Password))
            .Returns(fakePasswordHash);

        var userService =
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object,
                _passwordHasherMock.Object);

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Register(userDto);

        // Assert
        // uvek koristimo Assert.That metodu
        Assert.That(isError, Is.False);
        Assert.That(authResponse, Is.Not.Null);
        Assert.That(authResponse.Id, Is.EqualTo(generatedId));
        Assert.That(authResponse.Username, Is.EqualTo(userDto.Username));
        Assert.That(authResponse.Email, Is.EqualTo(userDto.Email));
        Assert.That(authResponse.PhoneNumber, Is.EqualTo(userDto.PhoneNumber));
        Assert.That(authResponse.Role, Is.EqualTo(UserRole.User));
        Assert.That(authResponse.Token, Is.EqualTo(fakeJwt));

        _passwordHasherMock.Verify(hasher => hasher.HashPassword(It.IsAny<User>(), userDto.Password), Times.Once);

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
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object,
                _passwordHasherMock.Object);

        var userDto = new CreateUserDTO()
        {
            Username = "some invalid ? #username",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Register(userDto);

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
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var userService =
            new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object,
                _passwordHasherMock.Object);

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Register(userDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Već postoji korisnik sa unetim korisničkim imenom."));

        _usersCollectionMock.Verify(c => c.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Login

    [Test]
    public async Task Login_ShouldLoginUser_WhenCredentialsAreValid()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var loginRequest = new LoginRequestDTO()
        {
            Email = existingUser.Email,
            Password = "123"
        };

        _passwordHasherMock.Setup(hasher =>
                hasher.VerifyHashedPassword(It.IsAny<User>(), existingUser.PasswordHash, loginRequest.Password))
            .Returns(PasswordVerificationResult.Success);

        const string fakeJwt = "some-jwt";
        _tokenServiceMock.Setup(service => service.CreateToken(It.IsAny<User>()))
            .Returns(fakeJwt);

        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object,
            _passwordHasherMock.Object);

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Login(loginRequest);

        //Assert
        Assert.That(isError, Is.False);
        Assert.That(authResponse, Is.Not.Null);
        Assert.That(authResponse.Id, Is.EqualTo(existingUser.Id));
        Assert.That(authResponse.Username, Is.EqualTo(existingUser.Username));
        Assert.That(authResponse.Email, Is.EqualTo(existingUser.Email));
        Assert.That(authResponse.PhoneNumber, Is.EqualTo(existingUser.PhoneNumber));
        Assert.That(authResponse.Role, Is.EqualTo(UserRole.User));
        Assert.That(authResponse.Token, Is.EqualTo(fakeJwt));

        _passwordHasherMock.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), existingUser.PasswordHash, loginRequest.Password),
            Times.Once);

        _tokenServiceMock.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task Login_ShouldReturnError_WhenEmailDoesNotExist()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User>());

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var loginRequest = new LoginRequestDTO()
        {
            Email = "non-existent@gmail.com",
            Password = "123"
        };

        var userService = new UserService(
            _usersCollectionMock.Object,
            _tokenServiceMock.Object,
            _serviceProviderMock.Object,
            _passwordHasherMock.Object);

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Login(loginRequest);

        //Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(403));
        Assert.That(error.Message, Is.EqualTo("Neispravan email ili lozinka."));

        _passwordHasherMock.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), existingUser.PasswordHash, loginRequest.Password),
            Times.Never);

        _tokenServiceMock.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task Login_ShouldReturnError_WhenPasswordIsIncorrect()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var loginRequest = new LoginRequestDTO()
        {
            Email = existingUser.Email,
            Password = "123"
        };

        _passwordHasherMock.Setup(hasher =>
                hasher.VerifyHashedPassword(It.IsAny<User>(), existingUser.PasswordHash, loginRequest.Password))
            .Returns(PasswordVerificationResult.Failed);

        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object,
            _passwordHasherMock.Object);

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await userService.Login(loginRequest);

        //Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(403));
        Assert.That(error.Message, Is.EqualTo("Neispravan email ili lozinka."));

        _passwordHasherMock.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), existingUser.PasswordHash, loginRequest.Password),
            Times.Once);

        _tokenServiceMock.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region GetCurrentUserId

    [Test]
    public void GetCurrentUserId_ShouldReturnUserId_WhenUserHasValidClaim()
    {
        // Arrange
        var expectedUserId = "some-id";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);
        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userId, ErrorMessage? error) = userService.GetCurrentUserId(user);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(userId, Is.EqualTo(expectedUserId));
        Assert.That(error, Is.Null);
    }

    [Test]
    public void GetCurrentUserId_ShouldReturnError_WhenUserHasNoNameIdentifierClaim()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);
        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userId, ErrorMessage? error) = userService.GetCurrentUserId(user);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(userId, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Korisnički ID nije pronađen, nedostaje NameIdentifier claim."));
    }

    [Test]
    public void GetCurrentUserId_ShouldReturnError_WhenClaimsPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? user = null;
        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object, _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userId, ErrorMessage? error) = userService.GetCurrentUserId(user);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(userId, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Korisnički podaci nisu dostupni, ClaimsPrincipal objekat je null."));
    }

    #endregion

    #region GetById

    [Test]
    public async Task GetById_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await userService.GetById("123");

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(userResult, Is.Not.Null);
        Assert.That(userResult.Id, Is.EqualTo(existingUser.Id));
        Assert.That(userResult.Username, Is.EqualTo(existingUser.Username));
        Assert.That(userResult.Email, Is.EqualTo(existingUser.Email));
        Assert.That(userResult.PhoneNumber, Is.EqualTo(existingUser.PhoneNumber));
        Assert.That(userResult.Role, Is.EqualTo(UserRole.User));

        _usersCollectionMock.Verify(collection => collection.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);

        _usersCursorMock.Verify(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task GetById_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User>());

        _usersCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await userService.GetById("non-existent-id");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));
    }

    [Test]
    public async Task GetById_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object, _passwordHasherMock.Object);

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await userService.GetById("123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja podataka o korisniku."));
    }

    #endregion
}