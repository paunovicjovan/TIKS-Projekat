namespace PlaywrightTests.APITests;

[TestFixture]
public class EstateControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _mainUserId = string.Empty;
    private string _estateId = string.Empty;
    private string _token = string.Empty;
    private string _estateAuthorToken = string.Empty;

    [SetUp]
    public async Task Setup()
    {
        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("Setup");
        Console.WriteLine("----------------------------------------------------------");

        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_token}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
    }

    [Test]
    [Order(1)]
    public async Task CreateEstate_ShouldReturnEstate_WhenCreationIsSuccessful()
    {
        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("Kreirana nekretnina");
        Console.WriteLine("----------------------------------------------------------");

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
            throw new Exception($"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("username", out var username) ?? false) &&
            (authResponse?.TryGetProperty("email", out var email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out var role) ?? false))
        {
            _mainUserId = id.GetString() ?? string.Empty;
            _estateAuthorToken = token.GetString() ?? string.Empty;
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

        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_estateAuthorToken}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

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

        response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            Multipart = formData,
            Headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_estateAuthorToken}" }
            }
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

    [Test]
    [Order(2)]
    public async Task CreateEstate_ShouldReturnError_WhenNoImagesAreProvided()
    {
        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
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

        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_estateAuthorToken}" }
        };

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            DataObject = estateData,
            Headers = headers
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var responseBody = await response.JsonAsync();

        if (responseBody?.TryGetProperty("detail", out var detail) ?? false)
        {
            Assert.That(detail.GetString(), Is.EqualTo("Nekretnina mora sadržati barem jednu sliku."));
        }
    }

    [Test]
    [Order(2)]
    public async Task CreateEstate_ShouldReturnError_WhenImageSavingFails()
    {
        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
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
            Images = new string[] { "invalid_image_format.txt" }
        };

        var headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_estateAuthorToken}" }
        };

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            DataObject = estateData,
            Headers = headers
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var responseBody = await response.JsonAsync();

        if (responseBody?.TryGetProperty("detail", out var detail) ?? false)
        {
            Assert.That(detail.GetString(), Is.EqualTo("Došlo je do greške prilikom kreiranja nekretnine."));
        }
    }

    // ZASTO NECE DA POZOVE OVOOOOOOOOOOOOOOOO

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("OneTimeTearDown");
        Console.WriteLine("----------------------------------------------------------");
    
        if (_request is not null)
        {
            try
            {
                if (!string.IsNullOrEmpty(_estateId))
                {
                    var deleteEstateResponse = await _request.DeleteAsync($"Estate/RemoveEstate/{_estateId}");
                    if (deleteEstateResponse.Status != 200)
                    {
                        throw new Exception($"Greška pri brisanju nekretnine: {deleteEstateResponse.Status}");
                    }
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
}