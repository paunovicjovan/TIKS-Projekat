using DataLayer.DTOs.Estate;

namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IMongoCollection<User>> _usersCollectionMock;
    private Mock<IAsyncCursor<User>> _usersCursorMock;
    private Mock<ITokenService> _tokenServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPasswordHasher<User>> _passwordHasherMock;
    private Mock<IEstateService> _estateServiceMock;
    private UserService _userService;

    [SetUp]
    public void SetUp()
    {
        _usersCollectionMock = new Mock<IMongoCollection<User>>();
        _usersCursorMock = new Mock<IAsyncCursor<User>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
        _estateServiceMock = new Mock<IEstateService>();
        _userService = new UserService(
            _usersCollectionMock.Object,
            _tokenServiceMock.Object,
            _serviceProviderMock.Object,
            _passwordHasherMock.Object
        );
    }

    #region Register

    [Test]
    //imenovanje: NazivMetodeKojaSeTestira_OcekivaniIshod_Uslov
    public async Task Register_ShouldRegisterUser_WhenUserIsValid()
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

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Register(userDto);

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
    public async Task Register_ShouldReturnError_WhenUsernameIsNotValid()
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

        var userDto = new CreateUserDTO()
        {
            Username = "some invalid ? #username",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Register(userDto);

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
    public async Task Register_ShouldReturnError_WhenUsernameIsTaken()
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

        // Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Register(userDto);

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

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Login(loginRequest);

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

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Login(loginRequest);

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

        //Act
        (bool isError, var authResponse, ErrorMessage? error) = await _userService.Login(loginRequest);

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
        var userService = new UserService(_usersCollectionMock.Object, _tokenServiceMock.Object,
            _serviceProviderMock.Object, _passwordHasherMock.Object);

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

        // Act
        (bool isError, var userId, ErrorMessage? error) = _userService.GetCurrentUserId(user);

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

        // Act
        (bool isError, var userId, ErrorMessage? error) = _userService.GetCurrentUserId(user);

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

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await _userService.GetById("123");

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

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await _userService.GetById("non-existent-id");

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

        // Act
        (bool isError, var userResult, ErrorMessage? error) = await _userService.GetById("123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja podataka o korisniku."));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_ShouldUpdateUser_WhenUserExistsAndUsernameIsValid(
        [Values("petar", "petar12", "_petar.")]
        string username)
    {
        //Arrange
        var updateDto = new UpdateUserDTO()
        {
            Username = username,
            PhoneNumber = "065 123 123"
        };

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

        var replaceOneResultMock = new Mock<ReplaceOneResult>();
        replaceOneResultMock.SetupGet(r => r.ModifiedCount).Returns(1);

        _usersCollectionMock.Setup(collection => collection.ReplaceOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<User>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceOneResultMock.Object);

        //Act
        (bool isError, var updateResult, ErrorMessage? error) = await _userService.Update(existingUser.Id, updateDto);

        //Assert
        Assert.That(isError, Is.False);
        Assert.That(updateResult, Is.Not.Null);
        Assert.That(updateResult.Username, Is.EqualTo(updateDto.Username));
        Assert.That(updateResult.PhoneNumber, Is.EqualTo(updateDto.PhoneNumber));

        _usersCollectionMock.Verify(collection => collection.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_ShouldReturnError_WhenUserDoesNotExist()
    {
        //Arrange
        var updateDto = new UpdateUserDTO()
        {
            Username = "petar",
            PhoneNumber = "065 123 123"
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

        const string userId = "123";
        //Act
        (bool isError, var updateResult, ErrorMessage? error) = await _userService.Update(userId, updateDto);

        //Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));

        _usersCollectionMock.Verify(collection => collection.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_ShouldReturnError_WhenUsernameIsNotValid(
        [Values("#1", "petar petar", "ime?")] string username)
    {
        //Arrange
        var updateDto = new UpdateUserDTO()
        {
            Username = username,
            PhoneNumber = "065 123 123"
        };

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

        //Act
        (bool isError, var updateResult, ErrorMessage? error) = await _userService.Update(existingUser.Id, updateDto);

        //Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message,
            Is.EqualTo(
                "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i ."));

        _usersCollectionMock.Verify(collection => collection.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddCommentToUser

    [Test]
    public async Task AddCommentToUser_ShouldAddComment_WhenUserExists()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddCommentToUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddCommentToUser_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddCommentToUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen ili nije ažuriran."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddCommentToUser_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error."));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddCommentToUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom dodavanja komentara korisniku."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RemoveCommentFromUser

    [Test]
    public async Task RemoveCommentFromUser_ShouldRemoveComment_WhenCommentExists()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var removeResult, ErrorMessage? error) =
            await _userService.RemoveCommentFromUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(removeResult, Is.True);
        Assert.That(error, Is.Null);

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveCommentFromUser_ShouldReturnError_WhenCommentNotFound()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var removeResult, ErrorMessage? error) =
            await _userService.RemoveCommentFromUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Komentar nije pronađen kod korisnika."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveCommentFromUser_ShouldReturnError_WhenExceptionIsThrown()
    {
        // Arrange
        const string userId = "123";
        const string commentId = "456";

        _usersCollectionMock
            .Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error."));

        // Act
        (bool isError, var removeResult, ErrorMessage? error) =
            await _userService.RemoveCommentFromUser(userId, commentId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom uklanjanja korisnikovog komentara."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddPostToUser

    [Test]
    public async Task AddPostToUser_ShouldAddPost_WhenUserExists()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddPostToUser(userId, postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);
        Assert.That(error, Is.Null);

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddPostToUser_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddPostToUser(userId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen ili nije ažuriran."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddPostToUser_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error."));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddPostToUser(userId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom dodavanja objave korisniku."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RemovePostFromUser

    [Test]
    public async Task RemovePostFromUser_ShouldRemovePost_WhenPostExists()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.RemovePostFromUser(userId, postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);
        Assert.That(error, Is.Null);

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemovePostFromUser_ShouldReturnError_WhenPostDoesNotExist()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.RemovePostFromUser(userId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Objava nije pronađena kod korisnika."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemovePostFromUser_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

        _usersCollectionMock.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error."));

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.RemovePostFromUser(userId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom uklanjanja korisnikove objave."));

        _usersCollectionMock.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddFavoriteEstate

    [Test]
    public async Task AddFavoriteEstate_ShouldAddFavoriteEstate_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212"
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

        _usersCollectionMock.Setup(collection => collection.ReplaceOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<User>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock.Setup(service => service.AddFavoriteUserToEstate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        const string estateId = "456";

        //Act
        (bool isError, var isSuccess, ErrorMessage? error) =
            await _userService.AddFavoriteEstate(existingUser.Id, estateId);

        //Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);
        Assert.That(error, Is.Null);

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _serviceProviderMock.Verify(provider => provider.GetService(typeof(IEstateService)), Times.Once);

        _estateServiceMock.Verify(service => service.AddFavoriteUserToEstate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task AddFavoriteEstate_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        const string userId = "123";
        const string postId = "456";

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

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.AddFavoriteEstate(userId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions<User>>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _serviceProviderMock.Verify(provider => provider.GetService(typeof(IEstateService)), Times.Never);

        _estateServiceMock.Verify(service => service.AddFavoriteUserToEstate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task AddFavoriteEstate_ShouldReturnError_WhenEstateIsAlreadyFavorite()
    {
        // Arrange
        const string estateId = "456";

        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212",
            FavoriteEstateIds = new List<string>() { estateId }
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

        //Act
        (bool isError, var isSuccess, ErrorMessage? error) =
            await _userService.AddFavoriteEstate(existingUser.Id, estateId);

        //Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nekretnina je već u omiljenim."));

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _serviceProviderMock.Verify(provider => provider.GetService(typeof(IEstateService)), Times.Never);

        _estateServiceMock.Verify(service => service.AddFavoriteUserToEstate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region RemoveFavoriteEstate

    [Test]
    public async Task RemoveFavoriteEstate_ShouldRemoveFavoriteEstate_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212",
            FavoriteEstateIds = new List<string> { "456" }
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

        _usersCollectionMock.Setup(collection => collection.ReplaceOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<User>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock
            .Setup(service => service.RemoveFavoriteUserFromEstate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        const string estateId = "456";

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) =
            await _userService.RemoveFavoriteEstate(existingUser.Id, estateId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(isSuccess, Is.True);
        Assert.That(error, Is.Null);

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _serviceProviderMock.Verify(provider => provider.GetService(typeof(IEstateService)), Times.Once);

        _estateServiceMock.Verify(
            service => service.RemoveFavoriteUserFromEstate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveFavoriteEstate_ShouldReturnError_WhenEstateIsNotFavorite()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212",
            FavoriteEstateIds = new List<string>()
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

        const string estateId = "456";

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) =
            await _userService.RemoveFavoriteEstate(existingUser.Id, estateId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nekretnina se ne nalazi u omiljenim."));

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RemoveFavoriteEstate_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        const string userId = "123";
        const string estateId = "456";

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

        // Act
        (bool isError, var isSuccess, ErrorMessage? error) = await _userService.RemoveFavoriteEstate(userId, estateId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));

        _usersCollectionMock.Verify(collection => collection.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CanAddToFavorite

    [Test]
    public async Task CanAddToFavorite_ShouldReturnTrue_WhenUserCanAddEstateToFavorites()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212",
            FavoriteEstateIds = new List<string>()
        };

        var estate = new EstateResultDTO()
        {
            Id = "456",
            Title = "Stan",
            Description = "Opis",
            Price = 100000,
            SquareMeters = 100,
            TotalRooms = 6,
            Category = EstateCategory.Flat,
            Images = new List<string>(),
            User = new UserResultDTO()
            {
                Id = "789",
                Email = "marko@gmail.com",
                PhoneNumber = "064 561 21 12",
                Username = "Marko"
            }
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

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock.Setup(service => service.GetEstate(estate.Id))
            .ReturnsAsync(estate);

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _userService.CanAddToFavorite(existingUser.Id, estate.Id);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task CanAddToFavorite_ShouldReturnError_WhenUserNotFound()
    {
        // Arrange
        const string userId = "123";
        const string estateId = "456";

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

        // Act
        (bool isError, var result, ErrorMessage? error) = await _userService.CanAddToFavorite(userId, estateId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen."));
    }

    [Test]
    public async Task CanAddToFavorite_ShouldReturnFalse_WhenUserIsOwnerOfEstate()
    {
        // Arrange
        var existingUser = new User()
        {
            Id = "123",
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123 123",
            PhoneNumber = "065 123 1212",
            FavoriteEstateIds = new List<string>()
        };

        var estate = new EstateResultDTO()
        {
            Id = "456",
            Title = "Stan",
            Description = "Opis",
            Price = 100000,
            SquareMeters = 100,
            TotalRooms = 6,
            Category = EstateCategory.Flat,
            Images = new List<string>(),
            User = new UserResultDTO(existingUser)
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

        _serviceProviderMock.Setup(provider => provider.GetService(typeof(IEstateService)))
            .Returns(_estateServiceMock.Object);

        _estateServiceMock.Setup(service => service.GetEstate(It.IsAny<string>()))
            .ReturnsAsync(estate);

        // Act
        (bool isError, var canAddToFavorite, ErrorMessage? error) =
            await _userService.CanAddToFavorite(existingUser.Id, estate.Id);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(canAddToFavorite, Is.False);
        Assert.That(error, Is.Null);
    }

    #endregion
}