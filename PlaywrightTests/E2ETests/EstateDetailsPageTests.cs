namespace PlaywrightTests.E2ETests;

[TestFixture]
public class EstateDetailsPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    // podaci prvog korisnika (vlasnik nekretnine)
    private readonly string _username1 = Guid.NewGuid().ToString("N");
    private readonly string _email1 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber1 = "065 123 1212";
    private string _user1Token = string.Empty;
    private string _estateId = string.Empty;

    // podaci drugog korisnika (posetilac nekretnine)
    private readonly string _username2 = Guid.NewGuid().ToString("N");
    private readonly string _email2 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber2 = "065 456 4545";
    private string _user2Token = string.Empty;

    private readonly string _password = "P@ssword123";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // kreiranje prvog korisnika koji je vlasnik nekretnine
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
            (authResponse?.TryGetProperty("username", out var username) ?? false) &&
            (authResponse?.TryGetProperty("email", out var email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out var role) ?? false))
        {
            _user1Token = token.GetString() ?? string.Empty;
        }

        // kreiranje drugog korisnika koji je posetilac nekretnine prvog korisnika
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        playwright = await Microsoft.Playwright.Playwright.CreateAsync();

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
            (authResponse?.TryGetProperty("username", out username) ?? false) &&
            (authResponse?.TryGetProperty("email", out email) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out phoneNumber) ?? false) &&
            (authResponse?.TryGetProperty("token", out token) ?? false) &&
            (authResponse?.TryGetProperty("role", out role) ?? false))
        {
            _user2Token = token.GetString() ?? string.Empty;
        }

        // kreiranje nekretnine prvog korisnika
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

        var estateData = new
        {
            Title = $"Luksuzna vila",
            Description = $"Vila sa bazenom",
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

        var estateResponse = await response.JsonAsync();

        if (estateResponse?.TryGetProperty("id", out var estateId) ?? false)
        {
            Console.WriteLine($"Nekretnina kreirana sa ID: {estateId.GetString()}");
            _estateId = estateId.GetString() ?? string.Empty;
        }
        else
        {
            throw new Exception($"Greška pri kreiranju nekretnine. Server nije vratio ID.");
        }

        // kreiranje objava prvog korisnika
        for (var i = 0; i < 8; i++)
        {
            await Request.PostAsync("Post/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Title = $"Naslov objave {i + 1}",
                    Content = $"Sadrzaj objave {i + 1}",
                    EstateId = _estateId
                }
            });
        }
    }

    [SetUp]
    public async Task Setup()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 1000
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
    public async Task CheckIfCorrectElementsAreDisplayedForEstateOwner()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email1);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).HoverAsync();
        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" }).First.HoverAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" }).First.ClickAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Ažuriraj" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator("button.btn.btn-outline-danger.me-2")).Not.ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator("h1")).ToContainTextAsync("Luksuzna vila");
        await Expect(PageWithSettings.Locator("p:has-text('Vila sa bazenom')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('500000 €')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('250 m²')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('Kuća')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("text='20'")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('N/A')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "065 123 1212" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = _username1 })).ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator(".leaflet-container")).ToBeVisibleAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave", Exact = true })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Kreiraj objavu" })).ToBeVisibleAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje" }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje" })).ToHaveCountAsync(8);
    }

    [Test]
    [Order(2)]
    public async Task CheckIfCorrectElementsAreDisplayedForEstateVisitor()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email2);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).HoverAsync();
        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" }).First.HoverAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" }).First.ClickAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Ažuriraj" })).Not.ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).Not.ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator("button.btn.btn-outline-danger.me-2")).ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator("h1")).ToContainTextAsync("Luksuzna vila");
        await Expect(PageWithSettings.Locator("p:has-text('Vila sa bazenom')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('500000 €')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('250 m²')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('Kuća')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("text='20'")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("p:has-text('N/A')")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "065 123 1212" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = _username1 })).ToBeVisibleAsync();

        await Expect(PageWithSettings.Locator(".leaflet-container")).ToBeVisibleAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave", Exact = true })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Kreiraj objavu" })).ToBeVisibleAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje" }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje" })).ToHaveCountAsync(8);
    }

    [TearDown]
    public async Task Teardown()
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
    public async Task Cleanup()
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