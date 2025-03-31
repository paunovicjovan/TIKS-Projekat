namespace PlaywrightTests.E2ETests;

[TestFixture]
public class UserProfilePageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    // podaci prvog korisnika (vlasnik profila)
    private string _user1Id = string.Empty;
    private readonly string _username1 = Guid.NewGuid().ToString("N");
    private readonly string _email1 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber1 = "065 123 1212";
    private string _user1Token = string.Empty;
    
    // podaci drugog korisnika (posetilac profila)
    private string _user2Id = string.Empty;
    private readonly string _username2 = Guid.NewGuid().ToString("N");
    private readonly string _email2 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber2 = "065 456 4545";
    private string _user2Token = string.Empty;
    
    private readonly string _password = "P@ssword123";
    private readonly List<string> _favoriteEstateIds = [];
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // kreiranje prvog korisnika koji ce da ima nekretnine i objave i ciji ce profil da se testira
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (Request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        var response = await Request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = _username1,
                Email = _email1,
                Password = _password,
                PhoneNumber = _phoneNumber1
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("username", out _) ?? false) &&
            (authResponse?.TryGetProperty("email", out _) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out _) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out _) ?? false))
        {
            _user1Id = id.GetString() ?? string.Empty;
            _user1Token = token.GetString() ?? string.Empty;
        }

        // kreiranje drugog korisnika koji je posetilac profila prvog korisnika
        
        response = await Request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = _username2,
                Email = _email2,
                Password = _password,
                PhoneNumber = _phoneNumber2
            }
        });

        if (response.Status != 200)
        {
            throw new Exception(
                $"Došlo je do greške pri kreiranju test podataka: {response.Status} - {response.StatusText}");
        }

        authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out id) ?? false) &&
            (authResponse?.TryGetProperty("username", out _) ?? false) &&
            (authResponse?.TryGetProperty("email", out _) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out _) ?? false) &&
            (authResponse?.TryGetProperty("token", out token) ?? false) &&
            (authResponse?.TryGetProperty("role", out _) ?? false))
        {
            _user2Id = id.GetString() ?? string.Empty;
            _user2Token = token.GetString() ?? string.Empty;
        }

        // kreiranje nekretnina prvog korisnika
        headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user1Token}" }
        };

        Request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        for (int j = 1; j <= 8; j++)
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

            var formData = Request.CreateFormData();

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

            response = await Request.PostAsync("Estate/CreateEstate", new APIRequestContextOptions
            {
                Multipart = formData
            });

            if (response.Status != 200)
            {
                throw new Exception($"Greška pri kreiranju test nekretnine {j}");
            }
        }

        // kreiranje objava prvog korisnika
        for (var i = 0; i < 8; i++)
        {
            await Request.PostAsync("Post/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Title = $"Naslov objave {i+1}",
                    Content = $"Sadrzaj objave {i+1}",
                    EstateId = (string?)null
                }
            });
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 2000
        });

        PageWithSettings = await BrowserWithSettings.NewPageAsync(new()
        {
            ViewportSize = new()
            {
                Width = 1280,
                Height = 720
            },
            ScreenSize = new()
            {
                Width = 1280,
                Height = 720
            }
        });
    }

    [Test]
    [Order(1)]
    public async Task CheckIfCorrectElementsAreDisplayedForProfileOwner()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email1);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username1.ToUpper() }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "MOJ PROFIL" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Podaci o korisniku" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_username1);
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_phoneNumber1);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Nekretnine korisnika" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni", Exact = true }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByTestId("add-to-favorite-btn")).Not.ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave korisnika" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true }).First).ToBeVisibleAsync();
    }

    [Test]
    [Order(2)]
    public async Task CheckIfCorrectElementsAreDisplayedForProfileVisitor()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email2);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        
        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Podaci o korisniku" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_username1);
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_phoneNumber1);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" })).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Nekretnine korisnika" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni", Exact = true }).First).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByTestId("add-to-favorite-btn").First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave korisnika" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true }).First).ToBeVisibleAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (PageWithSettings is null || BrowserWithSettings is null)
        {
            return;
        }

        await PageWithSettings.CloseAsync();
        await BrowserWithSettings.DisposeAsync();
        
        PageWithSettings = null;
        BrowserWithSettings = null;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // brisanje prvog korisnika
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user1Token}" }
        };

        Request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (Request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteUserResponse = await Request.DeleteAsync($"User/Delete");
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
            await Request.DisposeAsync();
            Request = null;
        }

        // brisanje drugog korisnika
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_user2Token}" }
        };

        Request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (Request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteUserResponse = await Request.DeleteAsync($"User/Delete");
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
            await Request.DisposeAsync();
            Request = null;
        }
    }
}