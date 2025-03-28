namespace PlaywrightTests.E2ETests;

[TestFixture]
public class FavoriteEstatesPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    private readonly string _email = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _password = "P@ssword123";
    private string _user1Token = string.Empty;
    private string _user2Token = string.Empty;
    private readonly List<string> _favoriteEstateIds = [];

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        //kreiranje prvog korisnika sa cijim nalogom se vrsi testiranje
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
                Username = Guid.NewGuid().ToString("N"),
                Email = _email,
                Password = _password,
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

        // kreiranje drugog korisnika koji ce da poseduje nekretnine koje ce prvi korisnik da doda u omiljene
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

        // kreiranje nekretnina drugog korisnika
        headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_user2Token}" }
        };

        Request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

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
                Console.WriteLine($"Nekretnina {j} kreirana sa ID: {estateId.GetString()}");

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

        Request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        foreach (var estateId in _favoriteEstateIds)
        {
            response = await Request.PostAsync($"User/AddToFavorites/{estateId}");

            if (response.Status == 200)
            {
                Console.WriteLine($"Nekretnina {estateId} dodata u omiljene prvom korisniku.");
            }
            else
            {
                Console.WriteLine($"Greška pri dodavanju nekretnine {estateId} u omiljene.");
            }
        }
    }

    [SetUp]
    public async Task Setup()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 5000
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

        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
    }

    [Test]
    [Order(1)]
    public async Task CheckIfAllElementsOnPageArePresent()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Omiljene nekretnine" })).ToBeVisibleAsync();

        var estateCards = PageWithSettings.Locator(".card");
        if (await estateCards.CountAsync() > 0)
        {
            await Expect(estateCards.First.Locator("img")).ToBeVisibleAsync();
            await Expect(estateCards.First.Locator("h5.text-blue")).ToBeVisibleAsync();
            await Expect(estateCards.First.Locator("p.text-golden")).ToBeVisibleAsync();
            await Expect(estateCards.First.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToBeVisibleAsync();
        }
        else
        {
            await Expect(PageWithSettings.Locator("p.text-muted")).ToHaveTextAsync("Korisnik trenutno nema omiljenih nekretnina.");
        }

        if (await PageWithSettings.Locator(".pagination").CountAsync() > 0)
        {
            await Expect(PageWithSettings.Locator(".pagination")).ToBeVisibleAsync();
        }
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