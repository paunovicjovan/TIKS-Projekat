namespace PlaywrightTests.E2ETests;

[TestFixture]
public class CreateEstatePageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    private readonly string _email = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _password = "P@ssword123";
    private string _userToken = string.Empty;
    private string _estateId = string.Empty;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
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
        
        // kreiranje korisnika sa cijeg naloga se vrsi testiranje
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
            _userToken = token.GetString() ?? string.Empty;
        }
        else
        {
            throw new Exception("Nisu pronađeni svi potrebni podaci u odgovoru pri kreiranju test korisnika.");
        }
    }
    
    [SetUp]
    public async Task Setup()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            //SlowMo = 1000
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
        
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "KREIRAJ NEKRETNINU", Exact = true }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Kreiraj Nekretninu" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("input[type=\"text\"]")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("textarea")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Combobox)).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Broj soba:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Površina:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Cena:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("input[type=\"file\"]")).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator(".leaflet-container")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Dodaj Nekretninu" })).ToBeVisibleAsync();
    }
    
    [Test]
    [Order(2)]
    public async Task CreateEstate_ShouldCreateEstate_WhenAllDataIsProvided()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/create-estate");
        await PageWithSettings.Locator("input[type=\"text\"]").ClickAsync();
        await PageWithSettings.Locator("input[type=\"text\"]").FillAsync("Test nekretnina");
        await PageWithSettings.Locator("textarea").ClickAsync();
        await PageWithSettings.Locator("textarea").FillAsync("Opis test nekretnine");
        await PageWithSettings.GetByRole(AriaRole.Combobox).SelectOptionAsync(new[] { "Flat" });
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Broj soba:$") }).GetByRole(AriaRole.Spinbutton).ClickAsync();
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Broj soba:$") }).GetByRole(AriaRole.Spinbutton).FillAsync("3");
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Površina:$") }).GetByRole(AriaRole.Spinbutton).ClickAsync();
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Površina:$") }).GetByRole(AriaRole.Spinbutton).FillAsync("60");
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton).ClickAsync();
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton).FillAsync("2");
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Cena:$") }).GetByRole(AriaRole.Spinbutton).ClickAsync();
        await PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Cena:$") }).GetByRole(AriaRole.Spinbutton).FillAsync("80000");
        await PageWithSettings.Locator("input[type=\"file\"]").SetInputFilesAsync([
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image1.jpg"), 
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image2.jpg"), 
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestImages", "image3.jpg")
        ]);
        
        await PageWithSettings.Locator(".leaflet-container").ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Dodaj Nekretninu" }).ClickAsync();
        
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex("http://localhost:5173/estate-page/.*"));
        var match = Regex.Match(PageWithSettings.Url, "http://localhost:5173/estate-page/([a-f0-9]+)$");

        if (match.Success)
        {
            _estateId = match.Groups[1].Value;
        }
        
        await Expect(PageWithSettings.Locator("h1")).ToContainTextAsync("Test nekretnina");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("80000 €");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("60 m²");
    }

    [Test]
    [Order(3)]
    public async Task CreateEstate_ShouldDisplayErrorMessage_WhenSomeDataIsMissing()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/create-estate");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Dodaj Nekretninu" }).ClickAsync();
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Unesite sve podatke!$") }).Nth(2)).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Unesite sve podatke!");
    }

    [Test]
    [Order(4)]
    public async Task CreateEstate_ShouldNotDisplayFloorNumberInput_WhenSelectedCategoryIsHouse()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/create-estate");
        await Expect(PageWithSettings.GetByRole(AriaRole.Combobox)).ToContainTextAsync("Kuća");
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton)).ToBeHiddenAsync();
        await PageWithSettings.GetByRole(AriaRole.Combobox).SelectOptionAsync(new[] { "Flat" });
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Combobox).SelectOptionAsync(new[] { "Office" });
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Combobox).SelectOptionAsync(new[] { "Retail" });
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton)).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Combobox).SelectOptionAsync(new[] { "House" });
        await Expect(PageWithSettings.Locator("div").Filter(new() { HasTextRegex = new Regex("^Sprat:$") }).GetByRole(AriaRole.Spinbutton)).ToBeHiddenAsync();
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
    public async Task OneTimeTearDown()
    {
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
            if (!string.IsNullOrEmpty(_estateId))
            {
                var deleteEstateResponse = await Request.DeleteAsync($"Estate/RemoveEstate/{_estateId}");
                if (deleteEstateResponse.Status != 200)
                {
                    throw new Exception($"Greška pri brisanju nekretnine: {deleteEstateResponse.Status}");
                }
            }
            
            var deleteUserResponse = await Request.DeleteAsync($"User/Delete");
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
            await Request.DisposeAsync();
            Request = null;
        }     
    }
}