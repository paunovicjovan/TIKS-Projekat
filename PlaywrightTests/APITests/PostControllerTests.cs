namespace PlaywrightTests.APITests;

[TestFixture]
public class PostControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _postId = string.Empty;
    private string _userToken = string.Empty;
    private string _estateId = string.Empty;
    private readonly string _testPostTitle = "Test Post Title";
    private readonly string _testPostContent = "Test Post Content";
    private bool _isPostAlreadyDeleted;
    private List<string> _userTokensToDelete = [];
    
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
            _userTokensToDelete.Add(_userToken);
        }

        response = await CreateTestEstate();

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

    private async Task<IAPIResponse> CreateTestEstate()
    {
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

        var formData = _request!.CreateFormData();

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

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        var response = await _request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
        {
            Multipart = formData
        });

        return response;
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
                Title = _testPostTitle,
                Content = _testPostContent,
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
    
    #region GetAllPosts
    
    [Test]
    public async Task GetAllPosts_ShouldReturnAtLeastOnePost_WhenPostsExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync("Post/GetAll");

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.AtLeast(1));
                Assert.That(data.EnumerateArray().Count(), Is.AtLeast(1));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetAllPosts_ShouldReturnCorrectPosts_WhenPostTitleIsProvided()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.GetAsync($"Post/GetAll?title={_testPostTitle}");

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.That(totalLength.GetInt64(), Is.GreaterThan(0));
            Assert.That(data.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(data.GetArrayLength(), Is.GreaterThan(0));

            foreach (var post in data.EnumerateArray())
            {
                if (post.TryGetProperty("title", out var title))
                {
                    Assert.That(title.GetString(), Is.Not.Null.And.Contains(_testPostTitle));
                }
                else
                {
                    Assert.Fail("Jedan od postova nema 'title'.");
                }
            }
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }
    
    [Test]
    public async Task GetAllPosts_ShouldReturnError_WhenPaginationParamsAreInvalid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.GetAsync($"Post/GetAll?page={-1}&pageSize={-1}");
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objava."));
    }
    
    #endregion
    
    #region GetPostById

    [Test]
    public async Task GetPostById_ShouldReturnPost_WhenPostExists()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync($"Post/GetById/{_postId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var post = await response.JsonAsync();

        if ((post?.TryGetProperty("id", out var id) ?? false) &&
            (post?.TryGetProperty("title", out var title) ?? false) &&
            (post?.TryGetProperty("content", out var content) ?? false) &&
            (post?.TryGetProperty("createdAt", out var createdAt) ?? false) &&
            (post?.TryGetProperty("author", out var author) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty & Is.EqualTo(_postId));
                Assert.That(title.GetString(), Is.EqualTo(_testPostTitle));
                Assert.That(content.GetString(), Is.EqualTo(_testPostContent));
                Assert.That(createdAt.GetDateTime(), Is.Not.EqualTo(default(DateTime)));
                Assert.That(author.ValueKind, Is.EqualTo(JsonValueKind.Object));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }
    
    [Test]
    public async Task GetById_ShouldReturnError_WhenPostDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string nonExistingPostId = "ffffffffffffffffffffffff";

        var response = await _request.GetAsync($"Post/GetById/{nonExistingPostId}");

        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Post nije pronađen."));
    }
    
    [Test]
    public async Task GetById_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string notValidObjectId = "not-valid-object-id";

        var response = await _request.GetAsync($"Post/GetById/{notValidObjectId}");
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objave."));
    }
    
    #endregion
    
    #region GetAllPostsForEstate

    [Test]
    [TestCase(1, 2, 2, 5)]
    [TestCase(2, 2, 2, 5)]
    [TestCase(3, 2, 1, 5)]
    [TestCase(4, 2, 0, 5)]
    public async Task GetAllPostsForEstate_ShouldReturnCorrectPaginatedPosts_WhenParamsAreValid(int page, int pageSize,
        int expectedCount, int totalPostsCount)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        // kreiranje test nekretnine
        var response = await CreateTestEstate();

        var estateResponse = await response.JsonAsync();

        string estateId;
        
        if (estateResponse?.TryGetProperty("id", out var estateIdElement) ?? false)
        {
            estateId = estateIdElement.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID nekretnine.");
        }
        
        // dodavanje objava nekretnini
        for (var i = 0; i < totalPostsCount; i++)
        {
            await _request.PostAsync("Post/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Title = $"Naslov objave {i+1}",
                    Content = $"Sadrzaj objave {i+1}",
                    EstateId = estateId
                }
            });
        }

        response = await _request.GetAsync($"Post/GetAllPostsForEstate/{estateId}?page={page}&pageSize={pageSize}");
        
        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(totalPostsCount));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(expectedCount));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetAllPostsForEstate_ShouldReturnEmptyList_WhenNoPostsExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await CreateTestEstate();

        var estateResponse = await response.JsonAsync();

        string estateId;
        
        if (estateResponse?.TryGetProperty("id", out var estateIdElement) ?? false)
        {
            estateId = estateIdElement.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID nekretnine.");
        }
        
        response = await _request.GetAsync($"Post/GetAllPostsForEstate/{estateId}");
        
        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(0));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(0));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetAllPostsForEstate_ShouldReturnError_WhenPaginationParamsAreInvalid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.GetAsync($"Post/GetAllPostsForEstate/{_estateId}?page={-1}&pageSize={-1}");
        
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objava."));
    }

    #endregion
    
    #region UpdatePost

    [Test]
    public async Task UpdatePost_ShouldReturnTrue_WhenUpdateIsSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newPostTitle = "Naslov objave 2";
        const string newPostContent = "Sadrzaj objave 2";

        var response = await _request.PutAsync($"Post/Update/{_postId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = newPostTitle,
                Content = newPostContent
            }
        });

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var isUpdateSuccessful = await response.JsonAsync<bool>();
        
        Assert.That(isUpdateSuccessful, Is.True);
    }

    [Test]
    public async Task UpdatePost_ShouldReturnError_WhenPostDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newPostTitle = "Naslov objave 2";
        const string newPostContent = "Sadrzaj objave 2";

        const string nonExistingPostId = "ffffffffffffffffffffffff"; 

        var response = await _request.PutAsync($"Post/Update/{nonExistingPostId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = newPostTitle,
                Content = newPostContent
            }
        });
        
        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Objava sa datim ID-jem ne postoji."));
    }

    [Test]
    public async Task UpdatePost_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newPostTitle = "Naslov objave 2";
        const string newPostContent = "Sadrzaj objave 2";

        const string invalidPostId = "invalid-object-id"; 

        var response = await _request.PutAsync($"Post/Update/{invalidPostId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Title = newPostTitle,
                Content = newPostContent
            }
        });
        
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom ažuriranja objave."));
    }

    #endregion
    
    #region DeletePost

    [Test]
    public async Task DeletePost_ShouldReturnTrue_WhenDeletionIsSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.DeleteAsync($"Post/Delete/{_postId}");

        Assert.That(response.Status, Is.EqualTo(204));
        Assert.That(response.StatusText, Is.EqualTo("No Content"));

        _isPostAlreadyDeleted = true;
    }

    [Test]
    public async Task DeletePost_ShouldReturnError_WhenPostDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string nonExistingPostId = "ffffffffffffffffffffffff";
        
        var response = await _request.DeleteAsync($"Post/Delete/{nonExistingPostId}");
        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Objava sa datim ID-jem ne postoji."));
    }

    [Test]
    public async Task DeletePost_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string notValidObjectId = "not-valid-object-id";
        
        var response = await _request.DeleteAsync($"Post/Delete/{notValidObjectId}");
        
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom brisanja objave."));
    }

    #endregion

    #region GetUserPosts

    [Test]
    [TestCase(1, 2, 2, 5)]
    [TestCase(2, 2, 2, 5)]
    [TestCase(3, 2, 1, 5)]
    [TestCase(4, 2, 0, 5)]
    public async Task GetUserPosts_ShouldReturnCorrectPaginatedPosts_WhenParamsAreValid(int page, int pageSize,
        int expectedCount, int totalPostsCount)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        // kreiranje novog korisnika
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

        string userId = string.Empty;
        string userToken = string.Empty;
        
        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false))
        {
            userId = id.GetString() ?? string.Empty;
            userToken = token.GetString() ?? string.Empty;
            _userTokensToDelete.Add(userToken);
        }
        
        // dodavanje objava korisniku
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {userToken}" }
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

        for (int i = 0; i < totalPostsCount; i++)
        {
            await _request.PostAsync("Post/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Title = $"Naslov objave {i+1}",
                    Content = $"Sadrzaj objave {i+1}",
                    EstateId = (string?) null
                }
            });
        }
        
        response = await _request.GetAsync($"Post/GetUserPosts/{userId}?page={page}&pageSize={pageSize}");
        
        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(totalPostsCount));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(expectedCount));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetUserPosts_ShouldReturnEmptyList_WhenNoPostsExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        // kreiranje novog korisnika
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

        string userId = string.Empty;
        
        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false))
        {
            userId = id.GetString() ?? string.Empty;
            _userTokensToDelete.Add(token.GetString() ?? string.Empty);
        }
        
        response = await _request.GetAsync($"Post/GetUserPosts/{userId}");
        
        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var paginatedPosts = await response.JsonAsync();
        
        if ((paginatedPosts?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedPosts?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(0));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(0));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetUserPosts_ShouldReturnError_WhenPaginationParamsAreInvalid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        // kreiranje novog korisnika
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

        string userId = string.Empty;
        string userToken = string.Empty;
        
        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false))
        {
            userId = id.GetString() ?? string.Empty;
            userToken = token.GetString() ?? string.Empty;
            _userTokensToDelete.Add(userToken);
        }
        
        response = await _request.GetAsync($"Post/GetUserPosts/{userId}?page={-1}&pageSize={-1}");
        
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja objava."));
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
        // brisanje test korisnika (sa njima i svi povezani podaci: nekretnine, postovi...)
        try
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            foreach (var userToken in _userTokensToDelete)   
            {
                var headers = new Dictionary<string, string>()
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", $"Bearer {userToken}" }
                };
                
                _request = await playwright.APIRequest.NewContextAsync(new()
                {
                    BaseURL = "http://localhost:5244/api/",
                    ExtraHTTPHeaders = headers,
                    IgnoreHTTPSErrors = true
                });

                if (_request is null)
                    throw new Exception("Greška u kontekstu.");
                
                var deletePostAuthorResponse = await _request.DeleteAsync("User/Delete");
                if (deletePostAuthorResponse.Status != 204)
                {
                    throw new Exception($"Greška pri brisanju test korisnika: {deletePostAuthorResponse.Status}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            if (_request is not null)
            {
                await _request.DisposeAsync();
                _request = null;
            }
        }
    }
}