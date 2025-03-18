namespace PlaywrightTests.APITests;

[TestFixture]
public class PostControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _postId = string.Empty;
    private string _userToken = string.Empty;
    private string _estateId = string.Empty;
    private bool _isPostAlreadyDeleted;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
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

        // kreiranje test korisnika ciji ce se token koristiti za kreiranje objave
        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = Guid.NewGuid().ToString("N"),
                Email = $"{Guid.NewGuid():N}@gmail.com",
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
            _userToken = token.GetString() ?? string.Empty;
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

        headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_userToken}" }
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
    public async Task SetUp()
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
        
        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Test post title",
                Content = "Hello World!",
                EstateId = (string?) null
            }
        });

        var postResponse = await response.JsonAsync();
        _isPostAlreadyDeleted = false;
        
        if (postResponse?.TryGetProperty("id", out var commentId) ?? false)
        {
            _postId = commentId.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID objave.");
        }
    }
    
    #region CreatePost

    [Test]
    public async Task CreatePost_ShouldReturnPost_WhenCreatingPostWithEstate()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string postTitle = "Naslov objave";
        const string postContent = "Sadrzaj objave";

        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = postTitle,
                Content = postContent,
                EstateId = _estateId
            }
        });

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var postResponse = await response.JsonAsync();
        
        if ((postResponse?.TryGetProperty("id", out var id) ?? false) &&
            (postResponse?.TryGetProperty("title", out var title) ?? false) &&
            (postResponse?.TryGetProperty("content", out var content) ?? false) &&
            (postResponse?.TryGetProperty("createdAt", out var createdAt) ?? false) &&
            (postResponse?.TryGetProperty("author", out var author) ?? false) &&
            (postResponse?.TryGetProperty("estate", out var estate) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(title.GetString(), Is.EqualTo(postTitle));
                Assert.That(content.GetString(), Is.EqualTo(postContent));
                Assert.That(createdAt.GetDateTime(), Is.Not.EqualTo(default(DateTime)));
                Assert.That(author.ValueKind, Is.EqualTo(JsonValueKind.Object));
                Assert.That(estate.ValueKind, Is.EqualTo(JsonValueKind.Object));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task CreatePost_ShouldReturnPost_WhenCreatingPostWithoutEstate()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string postTitle = "Naslov objave";
        const string postContent = "Sadrzaj objave";

        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = postTitle,
                Content = postContent,
                EstateId = (string?) null
            }
        });

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var postResponse = await response.JsonAsync();
        
        if ((postResponse?.TryGetProperty("id", out var id) ?? false) &&
            (postResponse?.TryGetProperty("title", out var title) ?? false) &&
            (postResponse?.TryGetProperty("content", out var content) ?? false) &&
            (postResponse?.TryGetProperty("createdAt", out var createdAt) ?? false) &&
            (postResponse?.TryGetProperty("author", out var author) ?? false) &&
            (postResponse?.TryGetProperty("estate", out var estate) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(title.GetString(), Is.EqualTo(postTitle));
                Assert.That(content.GetString(), Is.EqualTo(postContent));
                Assert.That(createdAt.GetDateTime(), Is.Not.EqualTo(default(DateTime)));
                Assert.That(author.ValueKind, Is.EqualTo(JsonValueKind.Object));
                Assert.That(estate.ValueKind, Is.EqualTo(JsonValueKind.Null));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task CreatePost_ShouldReturnError_WhenTokenIsInvalid()
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

        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = "Naslov objave",
                Content = "Sadrzaj objave",
                EstateId = (string?) null
            }
        });

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    #endregion

    [TearDown]
    public async Task TearDown()
    {
        if (_isPostAlreadyDeleted)
            return;
        
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
            var deletePostResponse = await _request.DeleteAsync($"Post/Delete/{_postId}");
            if (deletePostResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju test objave: {deletePostResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Došlo je do greške: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // brisanje test korisnika (sa njim i nekretnina)
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_userToken}" }
        };

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deletePostAuthorResponse = await _request.DeleteAsync("User/Delete");
            if (deletePostAuthorResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju test korisnika: {deletePostAuthorResponse.Status}");
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