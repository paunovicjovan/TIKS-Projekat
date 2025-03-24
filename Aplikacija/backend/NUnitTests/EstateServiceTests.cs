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
        Assert.That(error.StatusCode, Is.EqualTo(404));
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

        _userServiceMock.Setup(service => service.AddEstateToUser(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

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

        var estateDto = new EstateUpdateDTO
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
            Title = estateDto.Title,
            Description = estateDto.Description,
            Price = estateDto.Price,
            SquareMeters = estateDto.SquareMeters,
            TotalRooms = estateDto.TotalRooms,
            Category = estateDto.Category,
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
            await _estateService.UpdateEstate(estateId, estateDto);

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

        var estateDto = new EstateUpdateDTO
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
            await _estateService.UpdateEstate(estateId, estateDto);

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

        var estateDto = new EstateUpdateDTO
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
            await _estateService.UpdateEstate(estateId, estateDto);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(updatedEstateResult, Is.Null);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom ažuriranja nekretnine."));
    }

    #endregion

    #region RemoveEstate

    [Test]
    public async Task RemoveEstate_ShouldReturnTrue_WhenEstateExistsAndIsDeleted()
    {
        // Arrange
        const string estateId = "estate123";
        var estate = new Estate
        {
            Id = estateId,
            Title = "Nekretnina 1",
            Description = "Nekretnina u centru grada",
            Price = 150000.00,
            SquareMeters = 85,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Images = [],
            Longitude = 20.45689,
            Latitude = 44.81761,
            PostIds = ["post1", "post2"],
            FavoritedByUsersIds = ["user1", "user2"],
            UserId = "123"
        };

        _estatesCursorMock
            .SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _estatesCursorMock.SetupGet(x => x.Current).Returns(new List<Estate> { estate });

        _estatesCollectionMock
            .Setup(x => x.FindAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<FindOptions<Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _postServiceMock.Setup(x => x.DeletePost(It.IsAny<string>())).ReturnsAsync(true);
        _userServiceMock.Setup(x => x.RemoveFavoriteEstate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        _userServiceMock.Setup(service => service.RemoveEstateFromUser(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _estatesCollectionMock
            .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        (bool isError, bool result, ErrorMessage? error) = await _estateService.RemoveEstate(estateId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RemoveEstate_ShouldReturnError_WhenEstateDoesNotExist()
    {
        // Arrange
        const string estateId = "estate123";

        _estatesCursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _estatesCollectionMock
            .Setup(x => x.FindAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<FindOptions<Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        // Act
        (bool isError, _, ErrorMessage? error) = await _estateService.RemoveEstate(estateId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.Message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task RemoveEstate_ShouldReturnError_WhenDeleteFails()
    {
        // Arrange
        const string estateId = "estate123";
        var estate = new Estate
        {
            Id = estateId,
            Title = "Stan",
            Description = "Nekretnina u centru grada",
            Price = 150000.00,
            SquareMeters = 85,
            TotalRooms = 3,
            Category = EstateCategory.Flat,
            FloorNumber = 2,
            Images = [],
            Longitude = 20.45689,
            Latitude = 44.81761,
            PostIds = ["post1", "post2"],
            FavoritedByUsersIds = ["user1", "user2"],
            UserId = "123"
        };

        _estatesCursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _estatesCursorMock.SetupGet(c => c.Current)
            .Returns(new List<Estate> { estate });

        _estatesCollectionMock
            .Setup(x => x.FindAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<FindOptions<Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _postServiceMock.Setup(p => p.DeletePost(It.IsAny<string>())).ReturnsAsync(true);
        _userServiceMock.Setup(u => u.RemoveFavoriteEstate(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _estatesCollectionMock
            .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Greška prilikom brisanja"));

        // Act
        (bool isError, bool result, ErrorMessage? error) = await _estateService.RemoveEstate(estateId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom brisanja nekretnine i povezanih podataka."));
    }

    #endregion

    #region GetEstatesCreatedByUser

    [Test]
    [TestCase(1, 2, 2, 6)]
    [TestCase(2, 2, 2, 6)]
    [TestCase(3, 2, 2, 6)]
    [TestCase(4, 2, 0, 6)]
    public async Task GetEstatesCreatedByUser_ShouldReturnCorrectPaginatedEstates_WhenParamsAreValid(
        int page, int pageSize, int expectedCount, int totalEstatesCount)
    {
        // Arrange
        var userId = "123";
        var mockEstates = new List<Estate>();

        for (int i = 0; i < totalEstatesCount; i += 2)
        {
            mockEstates.Add(new Estate
            {
                Id = (i + 1).ToString(),
                Title = $"Estate {i + 1}",
                Description = $"Opis {i + 1}",
                Price = 100000 + (i * 1000),
                SquareMeters = 75 + i,
                TotalRooms = 3 + (i % 2),
                Category = EstateCategory.Flat,
                Images = new List<string> { "image1.jpg", "image2.jpg" },
                UserId = userId
            });

            mockEstates.Add(new Estate
            {
                Id = (i + 2).ToString(),
                Title = $"Estate {i + 2}",
                Description = $"Opis {i + 2}",
                Price = 100000 + ((i + 1) * 1000),
                SquareMeters = 75 + (i + 1),
                TotalRooms = 3 + ((i + 1) % 2),
                Category = EstateCategory.House,
                Images = new List<string> { "image3.jpg", "image4.jpg" },
                UserId = userId
            });
        }

        var expectedPaginatedEstates = mockEstates.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _estatesCursorMock.Setup(cursor => cursor.Current).Returns(expectedPaginatedEstates);

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _estatesCollectionMock
            .Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(totalEstatesCount);

        // Act
        (bool isError, var paginatedEstates, ErrorMessage? error) =
            await _estateService.GetEstatesCreatedByUser(userId, page, pageSize);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(paginatedEstates, Is.Not.Null);
        Assert.That(paginatedEstates.Data, Is.Not.Null);
        Assert.That(paginatedEstates.Data.Count, Is.EqualTo(expectedCount));
        Assert.That(paginatedEstates.TotalLength, Is.EqualTo(totalEstatesCount));
    }

    [Test]
    public async Task GetEstatesCreatedByUser_ShouldReturnEmptyList_WhenNoEstatesExist()
    {
        // Arrange
        var userId = "123";

        _estatesCursorMock.Setup(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _estatesCursorMock.Setup(cursor => cursor.Current).Returns(new List<Estate>());

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _estatesCollectionMock
            .Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(0);

        // Act
        (bool isError, var paginatedEstates, ErrorMessage? error) =
            await _estateService.GetEstatesCreatedByUser(userId, 1, 2);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(error, Is.Null);
        Assert.That(paginatedEstates, Is.Not.Null);
        Assert.That(paginatedEstates.Data, Is.Not.Null);
        Assert.That(paginatedEstates.Data.Count, Is.EqualTo(0));
        Assert.That(paginatedEstates.TotalLength, Is.EqualTo(0));
    }

    [Test]
    public async Task GetEstatesCreatedByUser_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "123";

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var paginatedEstates, ErrorMessage? error) =
            await _estateService.GetEstatesCreatedByUser(userId, 1, 2);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(paginatedEstates, Is.Null);
        Assert.That(error!.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja nekretnina."));
    }

    #endregion

    #region AddPostToEstate

    [Test]
    public async Task AddPostToEstate_ShouldReturnTrue_WhenPostIsAddedSuccessfully()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.AddPostToEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);
    }


    [Test]
    public async Task AddPostToEstate_ShouldReturnError_WhenEstateIsNotFoundOrNotUpdated()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.AddPostToEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nekretnina nije pronađena ili nije ažurirana."));
    }

    [Test]
    public async Task AddPostToEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.AddPostToEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom dodavanja objave kod nekretnine."));
    }

    #endregion

    #region RemovePostFromEstate

    [Test]
    public async Task RemovePostFromEstate_ShouldReturnTrue_WhenPostIsRemovedSuccessfully()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.RemovePostFromEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task RemovePostFromEstate_ShouldReturnError_WhenPostIsNotFoundInEstate()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.RemovePostFromEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Objava nije pronađena kod nekretnine."));
    }

    [Test]
    public async Task RemovePostFromEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var estateId = "123";
        var postId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.RemovePostFromEstate(estateId, postId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom uklanjanja objave sa nekretnine."));
    }

    #endregion

    #region SearchEstatesFilter

    [Test]
    [TestCase(0, 2, 2, 5)]
    [TestCase(2, 2, 2, 5)]
    [TestCase(4, 2, 1, 5)]
    [TestCase(6, 2, 0, 5)]
    public async Task SearchEstatesFilter_ShouldReturnPaginatedResults_WhenParamsAreValid(int skip, int limit,
        int expectedCount, int totalEstatesCount)
    {
        // Arrange
        var estates = new List<Estate>();
        for (int i = 0; i < totalEstatesCount; i++)
        {
            estates.Add(new Estate
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = $"Nekretnina {i + 1}",
                Description = $"Opis {i + 1}",
                Price = 100000 + (i * 50000),
                SquareMeters = 100 + (i * 10),
                TotalRooms = 3 + (i % 3),
                Category = EstateCategory.House,
                FloorNumber = i % 5,
                Images = ["image1.jpg", "image2.jpg"],
                Longitude = 45.2671,
                Latitude = 19.8335,
                UserId = ObjectId.GenerateNewId().ToString()
            });
        }

        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _estatesCursorMock.SetupGet(cursor => cursor.Current)
            .Returns(estates);

        _estatesCollectionMock.Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _estatesCollectionMock.Setup(x => x.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(totalEstatesCount);

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.SearchEstatesFilter(null, null, null, null, skip, limit);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(expectedCount));
        Assert.That(result.TotalLength, Is.EqualTo(totalEstatesCount));
    }

    [Test]
    public async Task SearchEstatesFilter_ShouldReturnEmptyResults_WhenNoMatchesFound()
    {
        // Arrange
        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _estatesCursorMock.SetupGet(cursor => cursor.Current)
            .Returns(new List<Estate>());

        _estatesCollectionMock.Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);
    
        _estatesCollectionMock.Setup(x => x.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(0);

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.SearchEstatesFilter();

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.TotalLength, Is.EqualTo(0));
    }
    
    [Test]
    public async Task SearchEstatesFilter_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _estatesCollectionMock.Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database failure"));

        // Act
        (bool isError, var result, ErrorMessage? error) = await _estateService.SearchEstatesFilter();

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom pretrage nekretnina."));
    }

    #endregion

    #region AddFavoriteUserToEstate

    [Test]
    public async Task AddFavoriteUserToEstate_ShouldReturnTrue_WhenEstateIsUpdatedSuccessfully()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.AddFavoriteUserToEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task AddFavoriteUserToEstate_ShouldReturnError_WhenEstateIsNotFoundOrNotUpdated()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.AddFavoriteUserToEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Nekretnina nije pronađena ili nije ažurirana."));
    }

    [Test]
    public async Task AddFavoriteUserToEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.AddFavoriteUserToEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message,
            Is.EqualTo("Došlo je do greške prilikom dodavanja korisnika kod omiljene nekretnine."));
    }

    #endregion

    #region RemoveFavoriteUserFromEstate

    [Test]
    public async Task RemoveFavoriteUserFromEstate_ShouldReturnTrue_WhenUserIsRemovedSuccessfully()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";

        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.RemoveFavoriteUserFromEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.True);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task RemoveFavoriteUserFromEstate_ShouldReturnError_WhenUserIsNotFoundInEstate()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";


        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.RemoveFavoriteUserFromEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message, Is.EqualTo("Korisnik nije pronađen kod nekretnine."));
    }

    [Test]
    public async Task RemoveFavoriteUserFromEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var estateId = "123";
        var userId = "456";
        _estatesCollectionMock
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Estate>>(), It.IsAny<UpdateDefinition<Estate>>(),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var result, ErrorMessage? error) =
            await _estateService.RemoveFavoriteUserFromEstate(estateId, userId);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(error.StatusCode, Is.EqualTo(400));
        Assert.That(error.Message,
            Is.EqualTo("Došlo je do greške prilikom uklanjanja korisnika sa omiljene nekretnine."));
    }

    #endregion

    #region GetUserFavoriteEstates

    [Test]
    [TestCase(1, 2, 2, 6)]
    [TestCase(2, 2, 2, 6)]
    [TestCase(3, 2, 2, 6)]
    [TestCase(4, 2, 0, 6)]
    public async Task GetUserFavoriteEstates_ShouldReturnCorrectPaginatedEstates_WhenParamsAreValid(
        int page, int pageSize, int expectedCount, int totalEstatesCount)
    {
        // Arrange
        var userId = "123";
        var mockEstates = new List<Estate>();

        for (int i = 0; i < totalEstatesCount; i += 2)
        {
            mockEstates.Add(new Estate
            {
                Id = (i + 1).ToString(),
                Title = $"Estate {i + 1}",
                Description = $"Opis {i + 1}",
                Price = 100000 + (i * 1000),
                SquareMeters = 75 + i,
                TotalRooms = 3 + (i % 2),
                Category = EstateCategory.Flat,
                Images = new List<string> { "image1.jpg", "image2.jpg" },
                UserId = userId
            });

            mockEstates.Add(new Estate
            {
                Id = (i + 2).ToString(),
                Title = $"Estate {i + 2}",
                Description = $"Opis {i + 2}",
                Price = 100000 + ((i + 1) * 1000),
                SquareMeters = 75 + (i + 1),
                TotalRooms = 3 + ((i + 1) % 2),
                Category = EstateCategory.House,
                Images = new List<string> { "image3.jpg", "image4.jpg" },
                UserId = userId
            });
        }

        var existingUser = new User()
        {
            Id = userId,
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User,
            FavoriteEstateIds = mockEstates
                .Select(e => e.Id)
                .Where(id => id != null)
                .Cast<string>()
                .ToList()
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var expectedPaginatedEstates = mockEstates.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        _estatesCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _estatesCursorMock.Setup(cursor => cursor.Current).Returns(expectedPaginatedEstates);

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _estatesCollectionMock
            .Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(totalEstatesCount);

        // Act
        (bool isError, var paginatedEstates, ErrorMessage? error) =
            await _estateService.GetUserFavoriteEstates(userId, page, pageSize);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(paginatedEstates, Is.Not.Null);
        Assert.That(paginatedEstates.Data, Is.Not.Null);
        Assert.That(paginatedEstates.Data.Count, Is.EqualTo(expectedCount));
        Assert.That(paginatedEstates.TotalLength, Is.EqualTo(totalEstatesCount));
    }

    [Test]
    public async Task GetUserFavoriteEstates_ShouldReturnEmptyList_WhenUserHasNoFavoriteEstates()
    {
        // Arrange
        var userId = "123";

        var existingUser = new User()
        {
            Id = userId,
            Username = "Petar",
            Email = "petar@gmail.com",
            PasswordHash = "123",
            PhoneNumber = "066 123 12 12",
            Role = UserRole.User,
            FavoriteEstateIds = new List<string>()
        };

        _usersCursorMock
            .SetupGet(cursor => cursor.Current)
            .Returns(new List<User> { existingUser });

        _usersCursorMock.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_usersCursorMock.Object);

        var expectedPaginatedEstates = new List<Estate>();

        _estatesCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<Estate>>(),
                It.IsAny<FindOptions<Estate, Estate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_estatesCursorMock.Object);

        _estatesCollectionMock
            .Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Estate>>(), null, default))
            .ReturnsAsync(0);

        // Act
        (bool isError, var paginatedEstates, ErrorMessage? error) =
            await _estateService.GetUserFavoriteEstates(userId, 1, 10);

        // Assert
        Assert.That(isError, Is.False);
        Assert.That(paginatedEstates, Is.Not.Null);
        Assert.That(paginatedEstates.Data, Is.Not.Null);
        Assert.That(paginatedEstates.Data.Count, Is.EqualTo(0));
        Assert.That(paginatedEstates.TotalLength, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserFavoriteEstates_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "123";

        _usersCollectionMock
            .Setup(collection => collection.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        (bool isError, var estates, ErrorMessage? error) =
            await _estateService.GetUserFavoriteEstates(userId, 1, 10);

        // Assert
        Assert.That(isError, Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(estates, Is.Null);
        Assert.That(error.Message, Is.EqualTo("Došlo je do greške prilikom preuzimanja omiljenih nekretnina."));
    }

    #endregion
}