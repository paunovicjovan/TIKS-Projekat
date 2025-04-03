namespace PlaywrightTests.E2ETests;

[TestFixture]
public class PostPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    // podaci prvog korisnika (sa cijeg naloga se izvrsava vecina testova)
    private string _user1Id = string.Empty;
    private readonly string _username1 = Guid.NewGuid().ToString("N");
    private readonly string _email1 = $"{Guid.NewGuid():N}@gmail.com";
    private string _user1Token = string.Empty;

    // podaci drugog korisnika (koji se koristi da proveri da se dugmici za izmenu i brisanje ne vide na tudjoj objavi)
    private string _user2Id = string.Empty;
    private readonly string _username2 = Guid.NewGuid().ToString("N");
    private readonly string _email2 = $"{Guid.NewGuid():N}@gmail.com";
    private string _user2Token = string.Empty;

    private readonly string _password = "P@ssword123";

    private string _postId = string.Empty;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // kreiranje prvog korisnika koji ce da ima objave i nekretninu
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
                PhoneNumber = "065 1313 123"
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

        // kreiranje jedne nekretnine
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

        if (response.Status != 200)
        {
            throw new Exception($"Greška pri kreiranju test nekretnine");
        }

        var estateResponse = await response.JsonAsync();
        var estateId = string.Empty;

        if (estateResponse?.TryGetProperty("id", out var estateIdFromJson) ?? false)
        {
            estateId = estateIdFromJson.GetString();
        }
        else
        {
            throw new Exception($"Greška pri kreiranju nekretnine. Server nije vratio ID.");
        }
        
        // kreiranje jedne objave sa nekretninom
        response = await Request.PostAsync("Post/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Naslov objave sa nekretninom",
                Content = "Sadrzaj objave sa nekretninom",
                EstateId = estateId
            }
        });
        
        if (response.Status != 200)
        {
            throw new Exception($"Greška pri kreiranju test objave");
        }

        var postResponse = await response.JsonAsync();

        if (postResponse?.TryGetProperty("id", out var postIdFromJson) ?? false)
        {
            _postId = postIdFromJson.GetString() ?? string.Empty;
        }
        else
        {
            throw new Exception($"Greška pri kreiranju objave. Server nije vratio ID.");
        }
        
        // dodavanje komentara na objavu
        for (int i = 0; i < 12; i++)
        {
            response = await Request.PostAsync("Comment/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Content = $"Komentar {i + 1}",
                    PostId = _postId
                }
            });

            if (response.Status != 200)
            {
                throw new Exception("DoŠlo je do greške pri kreiranju test komentara.");
            }
        }
        
        // kreiranje drugog korisnika
        response = await Request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = _username2,
                Email = _email2,
                Password = _password,
                PhoneNumber = "065 1313 123"
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
    }

    [SetUp]
    public async Task SetUp()
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
    public async Task CheckIfAllCorrectElementsAreDisplayedForPostOwner()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        // login sa nalogom autora objave
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email1);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
        
        await PageWithSettings.GotoAsync($"http://localhost:5173/forum/{_postId}");

        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Naslov objave sa nekretninom" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByText("Sadrzaj objave sa nekretninom")).ToBeVisibleAsync();
        
        // dugmici za izmenu i brisanje objave treba da budu vidljivi autoru objave
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni" }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        
        // treba u okviru objave da se vidi nekretnina na koju se objava odnosi
        await Expect(PageWithSettings.Locator(".card-body > .card")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Luksuzna vila" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByText("500000 €")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Izmeni$") }).GetByRole(AriaRole.Button)).ToBeVisibleAsync();
        
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite komentar" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Kreiraj komentar" })).ToBeDisabledAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Komentari" })).ToBeVisibleAsync();
        
        // dugmici za izmenu i brisanje komentara treba da budu vidljivi i da ih ima 5
        // (jer se toliko komentara ucitava na pocetku)
        await Expect(PageWithSettings.GetByTestId("edit-comment-btn")).ToHaveCountAsync(5);
        await Expect(PageWithSettings.GetByTestId("delete-comment-btn")).ToHaveCountAsync(5);
        
        // dugme za ucitavanje jos komentara
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prikaži još" })).ToBeVisibleAsync();
    }
    
    [Test]
    [Order(2)]
    public async Task CheckIfAllCorrectElementsAreDisplayedForPostVisitor()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        // login sa nalogom drugog korisnika (nije autor objave)
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email2);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
        
        await PageWithSettings.GotoAsync($"http://localhost:5173/forum/{_postId}");

        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Naslov objave sa nekretninom" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByText("Sadrzaj objave sa nekretninom")).ToBeVisibleAsync();
        
        // dugmici za izmenu i brisanje objave ne treba da budu vidljivi posetiocu
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni" }).First).Not.ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).Not.ToBeVisibleAsync();
        
        // treba u okviru objave da se vidi nekretnina na koju se objava odnosi
        await Expect(PageWithSettings.Locator(".card-body > .card")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Luksuzna vila" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByText("500000 €")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToBeVisibleAsync();
        
        // u ovom slucaju dugme za izmenu nekretnine se ne vidi
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Izmeni$") }).GetByRole(AriaRole.Button)).Not.ToBeVisibleAsync();
        
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite komentar" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Kreiraj komentar" })).ToBeDisabledAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Komentari" })).ToBeVisibleAsync();
        
        // dugmici za izmenu i brisanje komentara ne treba da budu vidljivi
        await Expect(PageWithSettings.GetByTestId("edit-comment-btn")).ToHaveCountAsync(0);
        await Expect(PageWithSettings.GetByTestId("delete-comment-btn")).ToHaveCountAsync(0);
        
        // dugme za ucitavanje jos komentara
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prikaži još" })).ToBeVisibleAsync();
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
        // brisanje prvog korisnika (automatski se brisu i svi ostali povezani entiteti)
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