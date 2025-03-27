namespace PlaywrightTests.E2ETests;

[TestFixture]
public class LoginPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }
    
    private readonly string _username = Guid.NewGuid().ToString("N");
    private readonly string _email = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _password = "@Petar123";
    private string _userToken = string.Empty;

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
        
        // kreiranje korisnika za login
        var response = await Request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = _username,
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
            Headless = false,
            SlowMo = 800
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
    public async Task CheckIfAllElementsAreDisplayed()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Prijavite Se" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Registrujte se." })).ToBeVisibleAsync();
    }
    
    [Test]
    [Order(2)]
    public async Task Login_ShouldLoginUser_WhenCredentialsAreCorrect()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username.ToUpper() })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#dropdown-basic")).ToContainTextAsync(_username.ToUpper());
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
    }

    [Test]
    [Order(3)]
    public async Task Login_ShouldDisplayErrorMessage_WhenEmailDoesNotExist()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        var nonExistingEmail = $"{Guid.NewGuid():N}@gmail.com";
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(nonExistingEmail);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Neispravan email ili lozinka.")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Neispravan email ili lozinka.");
    }
    
    [Test]
    [Order(4)]
    public async Task Login_ShouldDisplayErrorMessage_WhenPasswordIsIncorrect()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("incorrect password");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Neispravan email ili lozinka.")).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Neispravan email ili lozinka.");
    }
    
    [Test]
    [Order(5)]
    public async Task LogoutTest()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#dropdown-basic")).ToContainTextAsync(_username.ToUpper());
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username.ToUpper() }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "ODJAVI SE" })).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "ODJAVI SE" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "PRIJAVA" })).ToBeVisibleAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");
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