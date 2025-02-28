namespace NUnitTests;

[TestFixture]
public class EstateServiceTests
{
    private Mock<IMongoCollection<Estate>> _estatesCollectionMock;
    private Mock<IAsyncCursor<Estate>> _estatesCursorMock;
    private Mock<IMongoCollection<User>> _usersCollectionMock;
    private Mock<IAsyncCursor<User>> _usersCursorMock;
    private Mock<IUserService> _userServiceMock;
    private Mock<IPostService> _postServiceMock;
    private EstateService _estateService;

    [SetUp]
    public void SetUp()
    {
        _estatesCollectionMock = new Mock<IMongoCollection<Estate>>();
        _estatesCursorMock = new Mock<IAsyncCursor<Estate>>();
        _usersCollectionMock = new Mock<IMongoCollection<User>>();
        _usersCursorMock = new Mock<IAsyncCursor<User>>();
        _userServiceMock = new Mock<IUserService>();
        _postServiceMock = new Mock<IPostService>();
        _estateService = new EstateService(
            _estatesCollectionMock.Object,
            _usersCollectionMock.Object,
            _userServiceMock.Object,
            _postServiceMock.Object
        );
    }

    #region GetAllEstatesFromCollection

    [Test]
    public async Task GetAllEstatesFromCollection_ShouldReturnListOfEstates_WhenEstatesExist()
    {
        // Arrange
        var estates = new List<Estate>
        {
            new Estate
            {
                Id = "1",
                Title = "Estate 1",
                Description = "Opis1",
                Price = 100000,
                SquareMeters = 75,
                TotalRooms = 3,
                Category = EstateCategory.Flat,
                Images = ["image1.jpg", "image2.jpg"],
                UserId = "123"
            },
            new Estate
            {
                Id = "2",
                Title = "Estate 2",
                Description = "Opis2",
                Price = 100000,
                SquareMeters = 75,
                TotalRooms = 3,
                Category = EstateCategory.Flat,
                Images = ["image1.jpg", "image2.jpg"],
                UserId = "456"
            }
        };

        _estatesCursorMock.SetupGet(cursor => cursor.Current).Returns(estates);

        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _estatesCollectionMock.Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.GetAllEstatesFromCollection();

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("1"));
        Assert.That(result[1].Id, Is.EqualTo("2"));
    }

    [Test]
    public async Task GetAllEstatesFromCollection_ShouldReturnEmptyList_WhenNoEstatesExist()
    {
        // Arrange
        _estatesCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<Estate>());

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.GetAllEstatesFromCollection();

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllEstatesFromCollection_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _estatesCollectionMock.Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.GetAllEstatesFromCollection();

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja nekretnina."));
    }

    #endregion

    #region GetEstate

    [Test]
    public async Task GetEstate_ShouldReturnEstate_WhenEstateExists()
    {
        // Arrange
        var userId = "123";
        var estate = new Estate
        {
            Id = "1",
            Title = "Estate 1",
            Description = "Opis1",
            Price = 100000,
            SquareMeters = 75,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            Images = ["image1.jpg", "image2.jpg"],
            UserId = userId
        };

        _estatesCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<Estate> { estate });

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

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
        (bool isError, var estateResult, ErrorMessage? error) = await _estateService.GetEstate("estate123");

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(estateResult, Is.Not.Null);
        Assert.That(estateResult.Id, Is.EqualTo(estate.Id));
        Assert.That(estateResult.User, Is.Not.Null);
        Assert.That(estateResult.User.Id, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetEstate_ShouldReturnError_WhenEstateDoesNotExist()
    {
        // Arrange
        _estatesCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<Estate>());

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        // Act
        (bool isError, var estateResult, ErrorMessage? error) = await _estateService.GetEstate("invalid-id");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task GetEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database failure"));

        // Act
        (bool isError, var estateResult, ErrorMessage? error) = await _estateService.GetEstate("estate123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja nekretnine."));
    }

    #endregion

    #region CreateEstate

    [Test]
    public async Task CreateEstate_ShouldReturnEstate_WhenCreationIsSuccessful()
    {
        // Arrange

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var newEstateDto = new EstateCreateDTO
        {
            Title = "Stan",
            Description = "Opis stana",
            Price = 100000,
            SquareMeters = 80,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Images = [fileMock.Object, fileMock.Object],
            Longitude = 20.1234,
            Latitude = 44.5678
        };

        const string generatedEstateId = "123";

        _estatesCollectionMock.Setup(c =>
                c.InsertOneAsync(It.IsAny<Estate>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Estate, InsertOneOptions?, CancellationToken>((estate, _, _) =>
            {
                estate.Id = generatedEstateId;
            })
            .Returns(Task.CompletedTask);

        const string userId = "456";
        // Act
        (bool isError, var estateResult, ErrorMessage? error) = await _estateService.CreateEstate(newEstateDto, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(estateResult, Is.Not.Null);
        Assert.That(estateResult.Title, Is.EqualTo(newEstateDto.Title));
        Assert.That(estateResult.Description, Is.EqualTo(newEstateDto.Description));
        Assert.That(estateResult.Price, Is.EqualTo(newEstateDto.Price));
        Assert.That(estateResult.SquareMeters, Is.EqualTo(newEstateDto.SquareMeters));
        Assert.That(estateResult.TotalRooms, Is.EqualTo(newEstateDto.TotalRooms));
        Assert.That(estateResult.Category, Is.EqualTo(newEstateDto.Category));
        Assert.That(estateResult.FloorNumber, Is.EqualTo(newEstateDto.FloorNumber));
        Assert.That(estateResult.Images, Has.Count.EqualTo(newEstateDto.Images.Length));
        Assert.That(estateResult.Longitude, Is.EqualTo(newEstateDto.Longitude).Within(0.0001));
        Assert.That(estateResult.Latitude, Is.EqualTo(newEstateDto.Latitude).Within(0.0001));
        Assert.That(estateResult.UserId, Is.EqualTo(userId));

        _estatesCollectionMock.Verify(x => x.InsertOneAsync(It.IsAny<Estate>(), null, default), Times.Once);
    }

    [Test]
    public async Task CreateEstate_ShouldReturnError_WhenNoImagesAreProvided()
    {
        // Arrange
        var newEstateDto = new EstateCreateDTO
        {
            Title = "Stan",
            Description = "Opis stana",
            Price = 100000,
            SquareMeters = 80,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Longitude = 20.1234,
            Latitude = 44.5678,
            Images = []
        };

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.CreateEstate(newEstateDto, "123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nekretnina mora sadržati barem jednu sliku."));
        _estatesCollectionMock.Verify(x => x.InsertOneAsync(It.IsAny<Estate>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateEstate_ShouldReturnError_WhenImageSavingFails()
    {
        // Arrange
        var failingFileMock = new Mock<IFormFile>();
        failingFileMock.Setup(f => f.FileName).Returns("image.jpg");
        failingFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Greška pri snimanju fajla"));

        var newEstateDto = new EstateCreateDTO
        {
            Title = "Test Estate",
            Description = "Test Description",
            Price = 100000,
            SquareMeters = 80,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Longitude = 20.1234,
            Latitude = 44.5678,
            Images = [failingFileMock.Object]
        };

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.CreateEstate(newEstateDto, "123");

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom kreiranja nekretnine."));
        _estatesCollectionMock.Verify(x => x.InsertOneAsync(It.IsAny<Estate>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region UpdateEstate

    [Test]
    public async Task UpdateEstate_ShouldReturnUpdatedEstate_WhenSuccessful()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const string estateId = "123";
        const string ownerId = "456";

        var estateDTO = new EstateUpdateDTO
        {
            Title = "Estate 1",
            Description = "Opis1",
            Price = 100000,
            SquareMeters = 75,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            Images = [fileMock.Object, fileMock.Object]
        };

        var updatedEstate = new Estate
        {
            Id = estateId,
            Title = estateDTO.Title,
            Description = estateDTO.Description,
            Price = estateDTO.Price,
            SquareMeters = estateDTO.SquareMeters,
            TotalRooms = estateDTO.TotalRooms,
            Category = estateDTO.Category,
            Images = ["image1.jpg", "image2.jpg"],
            UserId = ownerId
        };

        _estatesCollectionMock
            .Setup(e => e.UpdateOneAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<UpdateDefinition<Estate>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _estatesCursorMock
            .Setup(cursor => cursor.Current)
            .Returns(new List<Estate>() { updatedEstate });

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        var owner = new UserResultDTO
        {
            Id = ownerId,
            Username = "Marko",
            Email = "marko@gmail.com",
            PhoneNumber = "063 1234 567"
        };

        _userServiceMock
            .Setup(u => u.GetById(It.IsAny<string>()))
            .ReturnsAsync(owner);

        // Act
        (bool isError, var updatedEstateResult, ErrorMessage? error) =
            await _estateService.UpdateEstate(estateId, estateDTO);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(updatedEstateResult, Is.Not.Null);
        Assert.That(updatedEstateResult.Id, Is.EqualTo(estateId));
        Assert.That(updatedEstateResult.UserId, Is.EqualTo(owner.Id));
        Assert.That(updatedEstateResult.Title, Is.EqualTo(updatedEstate.Title));
        Assert.That(updatedEstateResult.Description, Is.EqualTo(updatedEstate.Description));
        Assert.That(updatedEstateResult.Price, Is.EqualTo(updatedEstate.Price));
        Assert.That(updatedEstateResult.SquareMeters, Is.EqualTo(updatedEstate.SquareMeters));
        Assert.That(updatedEstateResult.TotalRooms, Is.EqualTo(updatedEstate.TotalRooms));
        Assert.That(updatedEstateResult.Category, Is.EqualTo(updatedEstate.Category));
        Assert.That(updatedEstateResult.Images, Is.EqualTo(updatedEstate.Images));

        _estatesCollectionMock.Verify(e => e.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Estate>>(),
            It.IsAny<Estate>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Test]
    public async Task UpdateEstate_ShouldReturnError_WhenEstateNotFound()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        const string estateId = "123";

        var estateDTO = new EstateUpdateDTO
        {
            Title = "Estate 1",
            Description = "Opis1",
            Price = 100000,
            SquareMeters = 75,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            Images = [fileMock.Object, fileMock.Object]
        };

        _estatesCursorMock
            .SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        // Act
        (bool isError, var updatedEstateResult, ErrorMessage? error) =
            await _estateService.UpdateEstate(estateId, estateDTO);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(updatedEstateResult, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task UpdateEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const string estateId = "123";

        var estateDTO = new EstateUpdateDTO
        {
            Title = "Estate 1",
            Description = "Opis1",
            Price = 100000,
            SquareMeters = 75,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            Images = [fileMock.Object, fileMock.Object]
        };

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database failure"));

        // Act
        (bool isError, var updatedEstateResult, ErrorMessage? error) =
            await _estateService.UpdateEstate(estateId, estateDTO);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(updatedEstateResult, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom ažuriranja nekretnine."));
    }

    #endregion
}