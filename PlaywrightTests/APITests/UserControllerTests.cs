﻿namespace PlaywrightTests.APITests;

[TestFixture]
public class UserControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _mainUserId = string.Empty;
    private string _secondaryUserId = string.Empty;
    private string _estateId = string.Empty;
    private string _username = Guid.NewGuid().ToString("N");
    private string _email = $"{Guid.NewGuid():N}@gmail.com";
    private string _password = "@Petar123";
    private string _phoneNumber = "065 123 1212";
    private string _mainUserToken = string.Empty;
    private string _estateAuthorToken = string.Empty;

    [OneTimeSetUp]
    public async Task CreateTestEstate()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();;
        
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
            _secondaryUserId = id.GetString() ?? string.Empty;
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
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_mainUserToken}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
    }

    #region Register
    
    [Test]
    [Order(1)]
    public async Task Register_ShouldRegisterUser_WhenUserIsValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

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

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
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
            _mainUserToken = token.GetString() ?? string.Empty;

            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(username.GetString(), Is.EqualTo(_username));
                Assert.That(email.GetString(), Is.EqualTo(_email));
                Assert.That(phoneNumber.GetString(), Is.EqualTo(_phoneNumber));
                Assert.That(token.GetString(), Is.Not.Empty);
                Assert.That(role.GetString(), Is.EqualTo("User"));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    [Order(2)]
    public async Task Register_ShouldReturnError_WhenUsernameIsNotValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = "some invalid ? #username",
                Email = _email,
                Password = _password,
                PhoneNumber = _phoneNumber
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message,
            Is.EqualTo(
                "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i ."));
    }

    [Test]
    [Order(3)]
    public async Task Register_ShouldReturnError_WhenUsernameIsTaken()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

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

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Već postoji korisnik sa unetim korisničkim imenom."));
    }
    
    #endregion

    #region Login

    [Test]
    [Order(4)]
    public async Task Login_ShouldLoginUser_WhenCredentialsAreValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync("User/Login", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Email = _email,
                Password = _password,
            }
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("username", out var username) ?? false) &&
            (authResponse?.TryGetProperty("email", out var email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out var role) ?? false))
        {
            _mainUserToken = token.GetString() ?? string.Empty;

            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(username.GetString(), Is.EqualTo(_username));
                Assert.That(email.GetString(), Is.EqualTo(_email));
                Assert.That(phoneNumber.GetString(), Is.EqualTo(_phoneNumber));
                Assert.That(token.GetString(), Is.Not.Empty);
                Assert.That(role.GetString(), Is.EqualTo("User"));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    [Order(5)]
    public async Task Login_ShouldReturnError_WhenEmailDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string nonExistingEmail = $"{Guid.NewGuid():N}@gmail.com";

        var response = await _request.PostAsync("User/Login", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Email = nonExistingEmail,
                Password = _password,
            }
        });

        Assert.That(response.Status, Is.EqualTo(403));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Neispravan email ili lozinka."));
    }

    [Test]
    [Order(6)]
    public async Task Login_ShouldReturnError_WhenPasswordIsIncorrect()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync("User/Login", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Email = _email,
                Password = "incorrectPassword",
            }
        });

        Assert.That(response.Status, Is.EqualTo(403));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Neispravan email ili lozinka."));
    }
    
    #endregion
    
    #region GetById

    [Test]
    [Order(7)]
    public async Task GetById_ShouldReturnUser_WhenUserExists()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync($"User/GetUserById/{_mainUserId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var user = await response.JsonAsync();

        if ((user?.TryGetProperty("id", out var id) ?? false) &&
            (user?.TryGetProperty("username", out var username) ?? false) &&
            (user?.TryGetProperty("email", out var email) ?? false) &&
            (user?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (user?.TryGetProperty("role", out var role) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty & Is.EqualTo(_mainUserId));
                Assert.That(username.GetString(), Is.EqualTo(_username));
                Assert.That(email.GetString(), Is.EqualTo(_email));
                Assert.That(phoneNumber.GetString(), Is.EqualTo(_phoneNumber));
                Assert.That(role.GetString(), Is.EqualTo("User"));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    [Order(8)]
    public async Task GetById_ShouldReturnError_WhenUserDoesNotExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string nonExistingUserId = "67aa51e1fb117ed44da18240";

        var response = await _request.GetAsync($"User/GetUserById/{nonExistingUserId}");

        Assert.That(response.Status, Is.EqualTo(404));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Korisnik nije pronađen."));
    }

    [Test]
    [Order(9)]
    public async Task GetById_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string notValidObjectId = "not-valid-object-id";

        var response = await _request.GetAsync($"User/GetUserById/{notValidObjectId}");

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja podataka o korisniku."));
    }

    #endregion
    
    #region Update
    
    [Test]
    [Order(10)]
    public async Task Update_ShouldUpdateUser_WhenUserExistsAndUsernameIsValid(
        [Values("petar", "petar12", "_petar.")]
        string newUsername)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newPhoneNumber = "065 123 123";

        var response = await _request.PutAsync("User/Update", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = newUsername,
                PhoneNumber = newPhoneNumber
            }
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var user = await response.JsonAsync();

        if ((user?.TryGetProperty("id", out var id) ?? false) &&
            (user?.TryGetProperty("username", out var username) ?? false) &&
            (user?.TryGetProperty("email", out var email) ?? false) &&
            (user?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (user?.TryGetProperty("role", out var role) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty & Is.EqualTo(_mainUserId));
                Assert.That(username.GetString(), Is.EqualTo(newUsername));
                Assert.That(email.GetString(), Is.EqualTo(_email));
                Assert.That(phoneNumber.GetString(), Is.EqualTo(newPhoneNumber));
                Assert.That(role.GetString(), Is.EqualTo("User"));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    [Order(11)]
    public async Task Update_ShouldReturnError_WhenTokenIsNotValid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_mainUserToken} not-valid" }
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

        var response = await _request.PutAsync("User/Update", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = "petar",
                PhoneNumber = "065 12 12 12"
            }
        });

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    [Test]
    [Order(12)]
    public async Task Update_ShouldReturnError_WhenUsernameIsNotValid(
        [Values("#1", "petar petar", "ime?")] string newUsername)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newPhoneNumber = "065 123 123";

        var response = await _request.PutAsync("User/Update", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = newUsername,
                PhoneNumber = newPhoneNumber
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message,
            Is.EqualTo(
                "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i ."));
    }
    
    #endregion
    
    #region AddToFavorites

    [Test]
    [Order(13)]
    public async Task AddFavoriteEstate_ShouldAddFavoriteEstate_WhenDataIsValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync($"User/AddToFavorites/{_estateId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var isSuccessful = await response.JsonAsync<bool>();

        Assert.That(isSuccessful, Is.True);
    }

    [Test]
    [Order(14)]
    public async Task AddFavoriteEstate_ShouldReturnError_WhenEstateIsAlreadyFavorite()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync($"User/AddToFavorites/{_estateId}");
        
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Nekretnina je već u omiljenim."));
    }
    
    [Test]
    [Order(15)]
    public async Task AddFavoriteEstate_ShouldReturnError_WhenTokenIsNotValid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_mainUserToken} not-valid" }
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

        var response = await _request.PostAsync($"User/AddToFavorites/{_estateId}");

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }
    
    #endregion
    
    #region RemoveFavoriteEstate

    [Test]
    [Order(16)]
    public async Task RemoveFavoriteEstate_ShouldRemoveFavoriteEstate_WhenDataIsValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.DeleteAsync($"User/RemoveFromFavorites/{_estateId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var isSuccessful = await response.JsonAsync<bool>();

        Assert.That(isSuccessful, Is.True);
    }

    [Test]
    [Order(17)]
    public async Task RemoveFavoriteEstate_ShouldReturnError_WhenEstateIsNotFavorite()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.DeleteAsync($"User/RemoveFromFavorites/{_estateId}");

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Nekretnina se ne nalazi u omiljenim."));
    }

    [Test]
    [Order(18)]
    public async Task RemoveFavoriteEstate_ShouldReturnError_WhenTokenIsNotValid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_mainUserToken} not-valid" }
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

        var response = await _request.DeleteAsync($"User/RemoveFromFavorites/{_estateId}");

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }
    
    #endregion
    
    #region CanAddToFavorite

    [Test]
    [Order(19)]
    public async Task CanAddToFavorite_ShouldReturnTrue_WhenUserCanAddEstateToFavorites()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync($"User/CanAddToFavorite/{_estateId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var isSuccessful = await response.JsonAsync<bool>();

        Assert.That(isSuccessful, Is.True);
    }
    
    [Test]
    [Order(20)]
    public async Task CanAddToFavorite_ShouldReturnError_WhenTokenIsNotValid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_mainUserToken} not-valid" }
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

        var response = await _request.GetAsync($"User/CanAddToFavorite/{_estateId}");

        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    [Test]
    [Order(21)]
    public async Task CanAddToFavorite_ShouldReturnFalse_WhenUserIsOwnerOfEstate()
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
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.GetAsync($"User/CanAddToFavorite/{_estateId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var canAddToFavorite = await response.JsonAsync<bool>();

        Assert.That(canAddToFavorite, Is.False);
    }
    
    #endregion

    [TearDown]
    public async Task End()
    {
        if (_request is not null)
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
            { "Authorization", $"Bearer {_estateAuthorToken}"}
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
        
        if (_request is not null)
        {
            // TODO: obrisati kreirane podatke
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