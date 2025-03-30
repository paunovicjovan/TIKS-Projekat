namespace PlaywrightTests.E2ETests;

[TestFixture]
public class RegisterPageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }
    
    // podaci korisnika koji se kreira u OneTimeSetUp, zbog provere registracije sa postojecim
    // korisnickim imenom i email-om
    private readonly string _username = Guid.NewGuid().ToString("N");
    private readonly string _email = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _password = "@Petar123";
    private string _userToken = string.Empty;
    
    // token korisnika koji se kreira u testu
    private string _registeredUserToken = string.Empty;

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
    public async Task SetUp()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            // SlowMo = 800
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
    public async Task CheckIfAllElementsOnPageArePresent()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Registrujte Se" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Prijavite se." })).ToBeVisibleAsync();
    }

    [Test]
    [Order(2)]
    public async Task Register_ShouldDisplayErrorMessage_WhenUsernameIsInvalid()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync("neispravno korisnicko ime");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 123 12312");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync("petar@gmail.com");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("@Petar123");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i .");
    }
    
    [Test]
    [Order(3)]
    public async Task Register_ShouldDisplayErrorMessage_WhenUsernameIsTaken()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync(_username);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 123 12312");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync("petar@gmail.com");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("@Petar123");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Već postoji korisnik sa unetim korisničkim imenom.");
    }
    
    [Test]
    [Order(4)]
    public async Task Register_ShouldDisplayErrorMessage_WhenEmailIsInvalid()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync("ime123");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 123 12312");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync("petar gmail");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("@Petar123");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Uneti e-mail nije validan.");
    }
    
    [Test]
    [Order(5)]
    public async Task Register_ShouldDisplayErrorMessage_WhenEmailIsTaken()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync("ime123");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 123 12312");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("@Petar123");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Već postoji korisnik sa unetim e-mail-om.");
    }
    
    [Test]
    [Order(6)]
    public async Task Register_ShouldDisplayErrorMessage_WhenPasswordIsWeak()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync("ime123");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 123 12312");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync("petar@gmail.com");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("password");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Lozinka mora da bude dužine barem 8 karaktera, da sadrži cifru, specijalni znak i veliko slovo.");
    }
    
    [Test]
    [Order(7)]
    public async Task RegisterForm_ShouldNotAcceptLettersInPhoneNumber()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("0");
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToHaveValueAsync("0");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("06");
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToHaveValueAsync("06");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("06a");
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToHaveValueAsync("06");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("06b");
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToHaveValueAsync("06");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("06 ");
        await Expect(PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" })).ToHaveValueAsync("06 ");
    }

    [Test]
    [Order(8)]
    public async Task Register_ShouldRegisterUser_WhenDataIsValid()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        var username = Guid.NewGuid().ToString("N");
        var email = $"{Guid.NewGuid():N}@gmail.com";
        
        await PageWithSettings.GotoAsync("http://localhost:5173/register");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite korisničko ime" }).FillAsync(username);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite broj telefona" }).FillAsync("065 12312 12");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(email);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync("P@ssword123");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173");
        await Expect(PageWithSettings.Locator("#dropdown-basic")).ToContainTextAsync(username.ToUpper());

        _registeredUserToken = await PageWithSettings.EvaluateAsync<string>("() => localStorage.getItem('token')");
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
        
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_registeredUserToken}" }
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