namespace PlaywrightTests.APITests;

[TestFixture]
public class EstateControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _user1Token = string.Empty;
    private string _user2Token = string.Empty;
    private string _user3Token = string.Empty;
    private List<string> _favoriteEstateIds = [];
    private string _estateId = string.Empty;
    private bool _isEstateAlreadyDeleted;

    [OneTimeSetUp]
    public async Task CreateTestUserAndEstate()
    {
        //kreiranje prvog korisnika
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = Guid.NewGuid().ToString("N"),
                Email = $"{Guid.NewGuid():N}@gmail12.com",
                Password = "P@ssword123",
                PhoneNumber = "065 123 1212"
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("username", out var username) ?? false) &&
            (authResponse?.TryGetProperty("email", out var email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out var role) ?? false))
        {
            _user1Token = token.GetString() ?? string.Empty;
        }

        // kreiranje drugog korisnika koji ce da poseduje 5 nekretnina
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = Guid.NewGuid().ToString("N"),
                Email = $"{Guid.NewGuid():N}@gmail12.com",
                Password = "P@ssword223",
                PhoneNumber = "065 123 2212"
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out id) ?? false) &&
            (authResponse?.TryGetProperty("username", out username) ?? false) &&
            (authResponse?.TryGetProperty("email", out email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out token) ?? false) &&
            (authResponse?.TryGetProperty("role", out role) ?? false))
        {
            _user2Token = token.GetString() ?? string.Empty;
        }

        // kreiranje nekretnina
        for (int j = 0; j < 5; j++)
        {
            var estateData = new
            {
                Title = $"Luksuzna vila {j}",
                Description = $"Vila sa bazenom {j}",
                Price = 500000 + (j * 10000),
                SquareMeters = 250 + (j * 10),
                TotalRooms = 20 + j,
                Category = 0,
                Longitude = 10.0 + j,
                Latitude = 20.0 + j,
                Images = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image1.jpg"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image2.jpg"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image3.jpg")
                }
            };

            var formData = _request.CreateFormData();

            formData.Append("Title", estateData.Title);
            formData.Append("Description", estateData.Description);
            formData.Append("Price", estateData.Price);
            formData.Append("SquareMeters", estateData.SquareMeters);
            formData.Append("TotalRooms", estateData.TotalRooms);
            formData.Append("Category", estateData.Category);
            formData.Append("Longitude", estateData.Longitude.ToString(CultureInfo.InvariantCulture));
            formData.Append("Latitude", estateData.Latitude.ToString(CultureInfo.InvariantCulture));

            for (var i = 0; i < estateData.Images.Length; i++)
            {
                formData.Append("Images", new FilePayload()
                {
                    Name = $"image{i}.jpg",
                    MimeType = "image/jpeg",
                    Buffer = await File.ReadAllBytesAsync(estateData.Images[i])
                });
            }

            headers = new Dictionary<string, string>()
            {
                { "Authorization", $"Bearer {_user2Token}" }
            };

            _request = await playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = "http://localhost:5244/api/",
                ExtraHTTPHeaders = headers,
                IgnoreHTTPSErrors = true
            });

            response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
            {
                Multipart = formData
            });

            var estateResponse = await response.JsonAsync();

            if (estateResponse?.TryGetProperty("id", out var estateId) ?? false)
            {
                Console.WriteLine($"Nekretnina {j} kreirana sa ID: {estateId.GetString()}");

                if (j < 3)
                    _favoriteEstateIds.Add(estateId.GetString()!);
            }
            else
            {
                throw new Exception($"Greška pri kreiranju nekretnine {j}. Server nije vratio ID.");
            }
        }

        // dodavanje omiljenih nekretnina prvom korisniku
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        foreach (var estateId in _favoriteEstateIds)
        {
            response = await _request.PostAsync($"User/AddToFavorites/{estateId}");

            if (response.Status == 200)
            {
                Console.WriteLine($"Nekretnina {estateId} dodata u omiljene prvom korisniku.");
            }
            else
            {
                Console.WriteLine($"Greška pri dodavanju nekretnine {estateId} u omiljene.");
            }
        }

        // kreiranje treceg korisnika
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = Guid.NewGuid().ToString("N"),
                Email = $"{Guid.NewGuid():N}@gmail12.com",
                Password = "P@ssword323",
                PhoneNumber = "065 123 3212"
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out id) ?? false) &&
            (authResponse?.TryGetProperty("username", out username) ?? false) &&
            (authResponse?.TryGetProperty("email", out email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out token) ?? false) &&
            (authResponse?.TryGetProperty("role", out role) ?? false))
        {
            _user3Token = token.GetString() ?? string.Empty;
        }
    }

    [SetUp]
    public async Task Setup()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        var estateData = new
        {
            Title = $"Luksuzna vila 123",
            Description = $"Vila sa bazenom 123",
            Price = 400000,
            SquareMeters = 200,
            TotalRooms = 15,
            Category = 0,
            Longitude = 30,
            Latitude = 35,
            Images = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image1.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image2.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image3.jpg")
            }
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", estateData.Title);
        formData.Append("Description", estateData.Description);
        formData.Append("Price", estateData.Price);
        formData.Append("SquareMeters", estateData.SquareMeters);
        formData.Append("TotalRooms", estateData.TotalRooms);
        formData.Append("Category", estateData.Category);
        formData.Append("Longitude", estateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", estateData.Latitude.ToString(CultureInfo.InvariantCulture));

        for (var i = 0; i < estateData.Images.Length; i++)
        {
            formData.Append("Images", new FilePayload()
            {
                Name = $"image{i}.jpg",
                MimeType = "image/jpeg",
                Buffer = await File.ReadAllBytesAsync(estateData.Images[i])
            });
        }

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            Multipart = formData
        });

        var estateResponse = await response.JsonAsync();
        _isEstateAlreadyDeleted = false;

        if (estateResponse?.TryGetProperty("id", out var estateId) ?? false)
        {
            _estateId = estateId.GetString()!;
        }
        else
        {
            throw new Exception($"Greška pri kreiranju nekretnine. Server nije vratio ID.");
        }
    }

    #region CreateEstate

    [Test]
    public async Task CreateEstate_ShouldReturnEstate_WhenCreationIsSuccessful()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var estateData = new
        {
            Title = "Luksuzna vila 345",
            Description = "Vila sa bazenom 345",
            Price = 400000,
            SquareMeters = 450,
            TotalRooms = 40,
            Category = 0,
            Longitude = 40.0,
            Latitude = 44.0,
            Images = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image1.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image2.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image3.jpg")
            }
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", estateData.Title);
        formData.Append("Description", estateData.Description);
        formData.Append("Price", estateData.Price);
        formData.Append("SquareMeters", estateData.SquareMeters);
        formData.Append("TotalRooms", estateData.TotalRooms);
        formData.Append("Category", estateData.Category);
        formData.Append("Longitude", estateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", estateData.Latitude.ToString(CultureInfo.InvariantCulture));

        for (var i = 0; i < estateData.Images.Length; i++)
        {
            formData.Append("Images", new FilePayload()
            {
                Name = $"image{i}.jpg",
                MimeType = "image/jpeg",
                Buffer = await File.ReadAllBytesAsync(estateData.Images[i])
            });
        }

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            Multipart = formData
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var estateResponse = await response.JsonAsync();

        if (estateResponse?.TryGetProperty("id", out var id) ?? false)
        {
            Assert.That(id.GetString(), Is.Not.Empty);
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task CreateEstate_ShouldReturnError_WhenNoImagesProvided()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var estateData = new
        {
            Title = "Luksuzna vila 678",
            Description = "Vila sa bazenom 678",
            Price = 400000,
            SquareMeters = 450,
            TotalRooms = 40,
            Category = 0,
            Longitude = 40.0,
            Latitude = 44.0,
            Images = new string[] { }
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", estateData.Title);
        formData.Append("Description", estateData.Description);
        formData.Append("Price", estateData.Price);
        formData.Append("SquareMeters", estateData.SquareMeters);
        formData.Append("TotalRooms", estateData.TotalRooms);
        formData.Append("Category", estateData.Category);
        formData.Append("Longitude", estateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", estateData.Latitude.ToString(CultureInfo.InvariantCulture));

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            Multipart = formData
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.JsonAsync();
        var errors = message?.GetProperty("errors").GetProperty("Images").EnumerateArray().FirstOrDefault();
        var errorMessage = errors?.ToString();
        Assert.That(errorMessage, Is.EqualTo("Nekretnina mora sadržati barem jednu sliku."));
    }

    [Test]
    public async Task CreateEstate_ShouldReturnError_WhenTokenIsInvalid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user1Token} not-valid" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Luksuzna vila 555",
                Description = "Vila sa bazenom 555",
                Price = 500000,
                SquareMeters = 250,
                TotalRooms = 20,
                Category = 0,
                Longitude = 10.0,
                Latitude = 20.0
            }
        });

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    #endregion

    #region GetEstateById

    [Test]
    public async Task GetEstateById_ShouldReturnEstate_WhenEstateExists()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync($"Estate/GetEstate/{_estateId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var estate = await response.JsonAsync();

        if ((estate?.TryGetProperty("id", out var id) ?? false) &&
            (estate?.TryGetProperty("title", out var title) ?? false) &&
            (estate?.TryGetProperty("description", out var description) ?? false) &&
            (estate?.TryGetProperty("price", out var price) ?? false) &&
            (estate?.TryGetProperty("squareMeters", out var squareMeters) ?? false) &&
            (estate?.TryGetProperty("totalRooms", out var totalRooms) ?? false) &&
            (estate?.TryGetProperty("category", out var category) ?? false) &&
            (estate?.TryGetProperty("longitude", out var longitude) ?? false) &&
            (estate?.TryGetProperty("latitude", out var latitude) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty & Is.EqualTo(_estateId));
                Assert.That(title.GetString(), Is.EqualTo("Luksuzna vila 123"));
                Assert.That(description.GetString(), Is.EqualTo("Vila sa bazenom 123"));
                Assert.That(price.GetInt32(), Is.EqualTo(400000));
                Assert.That(squareMeters.GetInt32(), Is.EqualTo(200));
                Assert.That(totalRooms.GetInt32(), Is.EqualTo(15));
                Assert.That(category.GetString(), Is.EqualTo("House"));
                Assert.That(longitude.GetInt32(), Is.EqualTo(30));
                Assert.That(latitude.GetInt32(), Is.EqualTo(35));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetEstateById_ShouldReturnError_WhenEstateDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string nonExistingEstateId = "ffffffffffffffffffffffff";

        var response = await _request.GetAsync($"Estate/GetEstate/{nonExistingEstateId}");

        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task GetEstateById_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string notValidObjectId = "not-valid-object-id";

        var response = await _request.GetAsync($"Estate/GetEstate/{notValidObjectId}");

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja nekretnine."));
    }

    #endregion

    #region UpdateEstate

    [Test]
    public async Task UpdatePost_ShouldReturnUpdatedEstate_WhenUpdateIsSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var newEstateData = new
        {
            Title = "Novi naziv vile 123",
            Description = "Novi opis vile 123",
            Price = 700000,
            SquareMeters = 700,
            TotalRooms = 30,
            Category = 1,
            Longitude = 54.0,
            Latitude = 58.0
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", newEstateData.Title);
        formData.Append("Description", newEstateData.Description);
        formData.Append("Price", newEstateData.Price);
        formData.Append("SquareMeters", newEstateData.SquareMeters);
        formData.Append("TotalRooms", newEstateData.TotalRooms);
        formData.Append("Category", newEstateData.Category);
        formData.Append("Longitude", newEstateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", newEstateData.Latitude.ToString(CultureInfo.InvariantCulture));

        var response = await _request.PutAsync($"Estate/UpdateEstate/{_estateId}", new APIRequestContextOptions()
        {
            Multipart = formData
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var updatedEstate = await response.JsonAsync();

        if ((updatedEstate?.TryGetProperty("id", out var id) ?? false) &&
            (updatedEstate?.TryGetProperty("title", out var title) ?? false) &&
            (updatedEstate?.TryGetProperty("description", out var description) ?? false) &&
            (updatedEstate?.TryGetProperty("price", out var price) ?? false) &&
            (updatedEstate?.TryGetProperty("squareMeters", out var squareMeters) ?? false) &&
            (updatedEstate?.TryGetProperty("totalRooms", out var totalRooms) ?? false) &&
            (updatedEstate?.TryGetProperty("category", out var category) ?? false) &&
            (updatedEstate?.TryGetProperty("longitude", out var longitude) ?? false) &&
            (updatedEstate?.TryGetProperty("latitude", out var latitude) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty & Is.EqualTo(_estateId));
                Assert.That(title.GetString(), Is.EqualTo("Novi naziv vile 123"));
                Assert.That(description.GetString(), Is.EqualTo("Novi opis vile 123"));
                Assert.That(price.GetInt32(), Is.EqualTo(700000));
                Assert.That(squareMeters.GetInt32(), Is.EqualTo(700));
                Assert.That(totalRooms.GetInt32(), Is.EqualTo(30));
                Assert.That(category.GetString(), Is.EqualTo("Flat"));
                Assert.That(longitude.GetInt32(), Is.EqualTo(54));
                Assert.That(latitude.GetInt32(), Is.EqualTo(58));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task UpdateEstate_ShouldReturnError_WhenEstateDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var newEstateData = new
        {
            Title = "Novi naziv vile 123",
            Description = "Novi opis vile 123",
            Price = 700000,
            SquareMeters = 700,
            TotalRooms = 30,
            Category = 1,
            Longitude = 54.0,
            Latitude = 58.0
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", newEstateData.Title);
        formData.Append("Description", newEstateData.Description);
        formData.Append("Price", newEstateData.Price);
        formData.Append("SquareMeters", newEstateData.SquareMeters);
        formData.Append("TotalRooms", newEstateData.TotalRooms);
        formData.Append("Category", newEstateData.Category);
        formData.Append("Longitude", newEstateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", newEstateData.Latitude.ToString(CultureInfo.InvariantCulture));

        const string nonExistingEstateId = "ffffffffffffffffffffffff";

        var response = await _request.PutAsync($"Estate/UpdateEstate/{nonExistingEstateId}", new APIRequestContextOptions()
        {
            Multipart = formData
        });

        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task UpdateEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var newEstateData = new
        {
            Title = "Novi naziv vile 123",
            Description = "Novi opis vile 123",
            Price = 700000,
            SquareMeters = 700,
            TotalRooms = 30,
            Category = 1,
            Longitude = 54.0,
            Latitude = 58.0
        };

        var formData = _request.CreateFormData();

        formData.Append("Title", newEstateData.Title);
        formData.Append("Description", newEstateData.Description);
        formData.Append("Price", newEstateData.Price);
        formData.Append("SquareMeters", newEstateData.SquareMeters);
        formData.Append("TotalRooms", newEstateData.TotalRooms);
        formData.Append("Category", newEstateData.Category);
        formData.Append("Longitude", newEstateData.Longitude.ToString(CultureInfo.InvariantCulture));
        formData.Append("Latitude", newEstateData.Latitude.ToString(CultureInfo.InvariantCulture));


        const string invalidEstateId = "invalid-object-id";

        var response = await _request.PutAsync($"Estate/UpdateEstate/{invalidEstateId}", new APIRequestContextOptions()
        {
            Multipart = formData
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom ažuriranja nekretnine."));
    }

    #endregion

    #region RemoveEstate

    [Test]
    public async Task DeleteEstate_ShouldReturnTrue_WhenDeletionIsSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.DeleteAsync($"Estate/RemoveEstate/{_estateId}");

        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));

        _isEstateAlreadyDeleted = true;
    }

    [Test]
    public async Task DeleteEstate_ShouldReturnError_WhenEstateDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string nonExistingEstateId = "ffffffffffffffffffffffff";

        var response = await _request.DeleteAsync($"Estate/RemoveEstate/{nonExistingEstateId}");

        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Nije pronađena nekretnina."));
    }

    [Test]
    public async Task DeleteEstate_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string notValidObjectId = "not-valid-object-id";

        var response = await _request.DeleteAsync($"Estate/RemoveEstate/{notValidObjectId}");

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom brisanja nekretnine i povezanih podataka."));
    }

    #endregion

    [TearDown]
    public async Task End()
    {
        if (_isEstateAlreadyDeleted)
            return;

        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteEstateResponse = await _request.DeleteAsync($"Estate/RemoveEstate/{_estateId}");
            if (deleteEstateResponse.Status != 200)
            {
                throw new Exception($"Greška pri brisanju nekretnine: {deleteEstateResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        // brisanje prvog korisnika
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user1Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteUserResponse = await _request.DeleteAsync($"User/Delete");
            if (deleteUserResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju korisnika: {deleteUserResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }

        // brisanje drugog korisnika
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user2Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteUserResponse = await _request.DeleteAsync($"User/Delete");
            if (deleteUserResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju korisnika: {deleteUserResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }

        // brisanje treceg korisnika
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user3Token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteUserResponse = await _request.DeleteAsync($"User/Delete");
            if (deleteUserResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju korisnika: {deleteUserResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }
    }
}