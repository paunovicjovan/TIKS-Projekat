namespace PlaywrightTests.APITests;

[TestFixture]
public class EstateControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _userId = string.Empty;
    private string _estateId = string.Empty;
    private string _userToken = string.Empty;
    private string _estateAuthorToken = string.Empty;
    private string _username = Guid.NewGuid().ToString("N");
    private string _email = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _password = "@Petar123";
    private readonly string _phoneNumber = "065 123 1212";

    [OneTimeSetUp]
    public async Task CreateTestUserAndEstate()
    {
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

        //kreiranje test korisnika koji poseduje nekretninu
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
            _estateAuthorToken = token.GetString() ?? string.Empty;
        }

        // kreiranje test nekretnine
        var estateData = new
        {
            Title = "Luksuzna vila",
            Description = "Vila sa bazenom",
            Price = 500000,
            SquareMeters = 250,
            TotalRooms = 20,
            Category = 0,
            Longitude = 10.0,
            Latitude = 20.0,
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
            { "Authorization", $"Bearer {_estateAuthorToken}" }
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
            _estateId = estateId.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID nekretnine.");
        }
    }

    [SetUp]
    public async Task Setup()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" }
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

        _username = Guid.NewGuid().ToString("N");
        _email = $"{Guid.NewGuid():N}@gmail.com";

        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = _username,
                Email = _email,
                Password = _password,
                PhoneNumber = _phoneNumber
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test korisnika: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("username", out var username) ?? false) &&
            (authResponse?.TryGetProperty("email", out var email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out var role) ?? false))
        {
            _userId = id.GetString() ?? string.Empty;
            _userToken = token.GetString() ?? string.Empty;
        }
        else
        {
            throw new Exception("Nisu pronađeni svi potrebni podaci u odgovoru pri kreiranju test korisnika.");
        }

        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_userToken}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
    }

    [Test]
    public async Task CreateEstate_ShouldReturnEstate_WhenCreationIsSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var estateData = new
        {
            Title = "Luksuzna vila",
            Description = "Vila sa bazenom",
            Price = 500000,
            SquareMeters = 250,
            TotalRooms = 20,
            Category = 0,
            Longitude = 10.0,
            Latitude = 20.0,
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

        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_userToken}" }
        };

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
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var estateData = new
        {
            Title = "Luksuzna vila",
            Description = "Vila sa bazenom",
            Price = 500000,
            SquareMeters = 250,
            TotalRooms = 20,
            Category = 0,
            Longitude = 10.0,
            Latitude = 20.0,
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

        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_userToken}" }
        };

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
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_userToken} not-valid" }
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
                Title = "Luksuzna vila",
                Description = "Vila sa bazenom",
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

    [TearDown]
    public async Task End()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_userToken}" }
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
                throw new Exception($"Greška pri brisanju test korisnika: {deleteUserResponse.Status}");
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
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_estateAuthorToken}" }
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
            if (!string.IsNullOrEmpty(_estateId))
            {
                var deleteEstateResponse = await _request.DeleteAsync($"Estate/RemoveEstate/{_estateId}");
                if (deleteEstateResponse.Status != 200)
                {
                    throw new Exception($"Greška pri brisanju test nekretnine: {deleteEstateResponse.Status}");
                }
            }

            var deleteEstateAuthorResponse = await _request.DeleteAsync($"User/Delete");
            if (deleteEstateAuthorResponse.Status != 204)
            {
                throw new Exception($"$Greška pri brisanju test korisnika: {deleteEstateAuthorResponse.Status}");
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