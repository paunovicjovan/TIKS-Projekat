﻿namespace PlaywrightTests.E2ETests;

[TestFixture]
public class ForumPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    // podaci prvog korisnika (vlasnik profila)
    private string _userId = string.Empty;
    private readonly string _username = Guid.NewGuid().ToString("N");
    private readonly string _email = $"{Guid.NewGuid():N}@gmail.com";
    private string _userToken = string.Empty;
    private readonly string _password = "P@ssword123";

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
                Username = _username,
                Email = _email,
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
            _userId = id.GetString() ?? string.Empty;
            _userToken = token.GetString() ?? string.Empty;
        }

        // kreiranje jedne nekretnine
        headers = new Dictionary<string, string>()
        {
            { "Authorization", $"Bearer {_userToken}" }
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

        // kreiranje objava bez nekretnine
        for (var i = 1; i <= 7; i++)
        {
            await Request.PostAsync("Post/Create", new APIRequestContextOptions
            {
                DataObject = new
                {
                    Title = $"Naslov objave {i}",
                    Content = $"Sadrzaj objave {i}",
                    EstateId = (string?)null
                }
            });
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
    }

    [SetUp]
    public async Task SetUp()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            // SlowMo = 0
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
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
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

        await PageWithSettings.GotoAsync("http://localhost:5173/forum");
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Kreiraj Objavu" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Naslov:" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Sadržaj:" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Pretražite objave po naslovu" }))
            .ToBeVisibleAsync();
    }

    [Test]
    [Order(2)]
    public async Task CreatePost_ShouldDisplayErrorMessage_WhenDataIsInvalid()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        // oba polja prazna
        await PageWithSettings.GotoAsync("http://localhost:5173/forum");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Molimo vas da popunite sve obavezne podatke.");
        await Task.Delay(5000); // cekanje da se alert skloni

        // sadrzaj prazan
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Naslov:" }).FillAsync("Naslov");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Molimo vas da popunite sve obavezne podatke.");
        await Task.Delay(5000);

        // naslov prazan
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Naslov:" }).FillAsync("");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Sadržaj:" }).FillAsync("Sadrzaj");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Molimo vas da popunite sve obavezne podatke.");
    }

    [Test]
    [Order(3)]
    public async Task CreatePost_ShouldCreatePost_WhenDataIsValid()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await PageWithSettings.GotoAsync("http://localhost:5173/forum");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Naslov:" })
            .FillAsync("Naslov najnovije objave");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Sadržaj:" })
            .FillAsync("Sadrzaj najnovije objave");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Objavi" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("Naslov najnovije objave");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("Sadrzaj najnovije objave");
    }

    [Test]
    [Order(4)]
    public async Task SeePostDetails_ShouldRedirectUserToPostPage_WhenButtonIsClicked()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await PageWithSettings.GotoAsync("http://localhost:5173/forum");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje" }).First.ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex("http://localhost:5173/forum/[a-f0-9]{24}"));
    }

    [Test]
    [Order(5)]
    public async Task SearchPostsByTitle_ShouldFilterPosts_WhenTitleIsEntered()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/forum");

        // objava bez nekretnine
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Pretražite objave po naslovu" })
            .FillAsync("Naslov objave 1");
        await Expect(PageWithSettings.Locator("h3").First).ToContainTextAsync("Naslov objave 1");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("Sadrzaj objave 1");

        // objava sa nekretninom
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Pretražite objave po naslovu" })
            .FillAsync("Naslov objave sa nekretninom");
        await Expect(PageWithSettings.Locator("h3").First).ToContainTextAsync("Naslov objave sa nekretninom");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("Sadrzaj objave sa nekretninom");
        await Expect(PageWithSettings.Locator("h5").First).ToContainTextAsync("Luksuzna vila");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("500000 €");
    }

    [Test]
    [Order(6)]
    public async Task PostPaginationChange_ShouldCountPostsOnPage_WhenCountOfPostsPerPageChange()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        await PageWithSettings.GotoAsync($"http://localhost:5173/forum");

        // 8 nekretnina je kreirano pre testova
        var buttonsLocator = PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true });
        
        // provera za 10 objava po stranici
        var postsCount = await GetElementCountWithRetry(PageWithSettings, buttonsLocator);
        Assert.That(postsCount, Is.InRange(8, 10));

        // provera za 5 objava po stranici
        await PageWithSettings.GetByRole(AriaRole.Combobox).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "5" }).ClickAsync();
        postsCount = await GetElementCountWithRetry(PageWithSettings, buttonsLocator);
        Assert.That(postsCount, Is.EqualTo(5));

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();
        postsCount = await GetElementCountWithRetry(PageWithSettings, buttonsLocator);
        Assert.That(postsCount, Is.InRange(3, 5));

        // provera za 20 objava po stranici
        await PageWithSettings.GetByRole(AriaRole.Combobox).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "20" }).ClickAsync();
        postsCount = await GetElementCountWithRetry(PageWithSettings, buttonsLocator);
        Assert.That(postsCount, Is.InRange(8, 20));
    }

    private async Task<int> GetElementCountWithRetry(IPage page, ILocator locator, int maxRetries = 5,
        int delayMs = 500)
    {
        int count = 0;
        for (int i = 0; i < maxRetries; i++)
        {
            count = await locator.CountAsync();
            if (count > 0)
                return count;
            await Task.Delay(delayMs);
        }

        return count;
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
            { "Authorization", $"Bearer {_userToken}" }
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