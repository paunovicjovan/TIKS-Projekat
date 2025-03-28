namespace PlaywrightTests.E2ETests;

[TestFixture]
public class HomePageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }

    [SetUp]
    public async Task Setup()
    {
        BrowserWithSettings = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 4000
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
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await Expect(PageWithSettings.Locator("span")).ToContainTextAsync("Domovida");
        await Expect(PageWithSettings.Locator("h1")).ToContainTextAsync("Tvoj idealan dom čeka na tebe!");
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Pronađi Nekretninu" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Pregledaj Nekretnine" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Kreiraj Nekretninu" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Podeli Mišljenje" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Registruj Se" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "O NAMA" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "PRIJAVA" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "REGISTRACIJA" })).ToBeVisibleAsync();
    }

    [Test]
    [Order(2)]
    public async Task CheckIfButtonsOnPageAreWorking()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Pronađi Nekretninu" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/search-estates");
        await Expect(PageWithSettings.Locator("h3")).ToContainTextAsync("Pretraga Nekretnina");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" })).ToBeVisibleAsync();
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Pregledaj Nekretnine" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/search-estates");
        await Expect(PageWithSettings.Locator("h3")).ToContainTextAsync("Pretraga Nekretnina");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" })).ToBeVisibleAsync();
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Kreiraj Nekretninu" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/login");
        await Expect(PageWithSettings.Locator("h4")).ToContainTextAsync("Prijavite Se");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" })).ToBeVisibleAsync();
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Podeli Mišljenje" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/login");
        await Expect(PageWithSettings.Locator("h4")).ToContainTextAsync("Prijavite Se");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" })).ToBeVisibleAsync();
        
        await PageWithSettings.GotoAsync("http://localhost:5173/");
        await PageWithSettings.GetByRole(AriaRole.Link, new() { Name = "Registruj Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/register");
        await Expect(PageWithSettings.Locator("h4")).ToContainTextAsync("Registrujte Se");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Registrujte Se" })).ToBeVisibleAsync();
    }

    [Test]
    [Order(3)]
    public async Task CheckIfLinksInNavbarAreWorking()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }
        
        await PageWithSettings.GotoAsync("http://localhost:5173");
        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "O NAMA" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/search-estates");

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "PRIJAVA" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/login");

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "REGISTRACIJA" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/register");
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
}