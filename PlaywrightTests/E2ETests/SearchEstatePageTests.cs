namespace PlaywrightTests.E2ETests;

[TestFixture]
public class SearchEstatePageTests : PageTest
{
    private IBrowser? BrowserWithSettings { get; set; }
    private IPage? PageWithSettings { get; set; }
    private IAPIRequestContext? Request { get; set; }

    // podaci prvog korisnika (vlasnik nekretnina)
    private readonly string _username1 = Guid.NewGuid().ToString("N");
    private readonly string _email1 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber1 = "065 123 1212";
    private string _user1Token = string.Empty;
    private string _estateId = string.Empty;

    // podaci drugog korisnika (pretrazivac nekretnina)
    private readonly string _username2 = Guid.NewGuid().ToString("N");
    private readonly string _email2 = $"{Guid.NewGuid():N}@gmail.com";
    private readonly string _phoneNumber2 = "065 456 4545";
    private string _user2Token = string.Empty;

    private readonly string _password = "P@ssword123";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // kreiranje prvog korisnika koji je vlasnik nekretnina
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

        if ((authResponse?.TryGetProperty("id", out _) ?? false) &&
            (authResponse?.TryGetProperty("username", out _) ?? false) &&
            (authResponse?.TryGetProperty("email", out _) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out _) ?? false) &&
            (authResponse?.TryGetProperty("token", out var token) ?? false) &&
            (authResponse?.TryGetProperty("role", out _) ?? false))
        {
            _user1Token = token.GetString() ?? string.Empty;
        }

        // kreiranje drugog korisnika koji pretrazuje nekretnine
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

        if ((authResponse?.TryGetProperty("id", out _) ?? false) &&
            (authResponse?.TryGetProperty("username", out _) ?? false) &&
            (authResponse?.TryGetProperty("email", out _) ?? false) &&
            (authResponse?.TryGetProperty("phoneNumber", out _) ?? false) &&
            (authResponse?.TryGetProperty("token", out token) ?? false) &&
            (authResponse?.TryGetProperty("role", out _) ?? false))
        {
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

        for (int j = 1; j <= 20; j++)
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
                if(j == 1)
                    _estateId = estateId.GetString() ?? string.Empty;
            }
            else
            {
                throw new Exception($"Greška pri kreiranju nekretnine {j}. Server nije vratio ID.");
            }
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await Expect(PageWithSettings.Locator("h3")).ToContainTextAsync("Pretraga Nekretnina");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" })).ToBeVisibleAsync();
        
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni" })).ToHaveCountAsync(10);

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni" })).ToHaveCountAsync(10);
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await Expect(PageWithSettings.Locator("h3")).ToContainTextAsync("Pretraga Nekretnina");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" })).ToBeVisibleAsync();
        
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.Locator("button.btn.btn-outline-danger.ms-2")).ToHaveCountAsync(10);

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();

        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);
        await Expect(PageWithSettings.Locator("button.btn.btn-outline-danger.ms-2")).ToHaveCountAsync(10);
    }

    [Test]
    [Order(3)]
    public async Task AddOrRemoveEstateFromFavorites_ShouldAddOrRemoveEstateFromFavorites_WhenButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        // dodavanje u omiljene
        await PageWithSettings.Locator("button.btn.btn-outline-danger.ms-2").First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Nekretnina je dodata u omiljene!");
        await Expect(PageWithSettings.Locator("button.btn.btn-danger.ms-2")).ToBeVisibleAsync();
        await Task.Delay(5000); // cekanje da se alert skloni

        // uklanjanje iz omiljenih
        await PageWithSettings.Locator("button.btn.btn-danger.ms-2").ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Nekretnina je uklonjena iz omiljenih!");
        await Expect(PageWithSettings.Locator("button.btn.btn-danger.ms-2")).Not.ToBeVisibleAsync();
    }

    [Test]
    [Order(4)]
    public async Task PaginationChange_ShouldCountEstatesOnPage_WhenCountOfEstatesPerPageChange()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        // provera za 10 nekretnina po stranici
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(10);

        // provera za 5 nekretnina po stranici
        await PageWithSettings.GetByText("10", new() { Exact = true }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "5" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(5);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(5);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(5);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(5);

        // provera za 20 nekretnina po stranici
        await PageWithSettings.GetByText("5", new() { Exact = true }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "20" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" })).ToHaveCountAsync(20);
    }

    [Test]
    [Order(5)]
    public async Task SearchEstates_ShouldFilterEstates_WhenFilterButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        // Provera ako se unese samo naziv
        await PageWithSettings.Locator("input.form-control").First.FillAsync("Luksuzna vila 14");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Luksuzna vila 14")).ToHaveCountAsync(1);

        // provera ako se unese naziv i minimalna cena
        await PageWithSettings.Locator("input.form-control").First.FillAsync("Luksuzna vila");
        await PageWithSettings.Locator("input.form-control").Nth(1).FillAsync("650000");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Luksuzna vila")).ToHaveCountAsync(6);

        // provera ako se unese naziv, minimalna i maximalna cena
        await PageWithSettings.Locator("input.form-control").First.FillAsync("Luksuzna vila");
        await PageWithSettings.Locator("input.form-control").Nth(1).FillAsync("650000");
        await PageWithSettings.Locator("input.form-control").Nth(2).FillAsync("680000");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Luksuzna vila")).ToHaveCountAsync(4);

        // provera ako se unese naziv, minimalna i maximalna cena i kategorija (kuca)
        await PageWithSettings.Locator("input.form-control").First.FillAsync("Luksuzna vila");
        await PageWithSettings.Locator("input.form-control").Nth(1).FillAsync("650000");
        await PageWithSettings.Locator("input.form-control").Nth(2).FillAsync("680000");
        await PageWithSettings.GetByRole(AriaRole.Checkbox, new() { Name = "Kuća" }).CheckAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pretraži" }).ClickAsync();
        await Expect(PageWithSettings.GetByText("Luksuzna vila")).ToHaveCountAsync(4);
    }

    [Test]
    [Order(6)]
    public async Task SeeEstateDetails_ShouldRedirectUserToEstateDetailsPage_WhenButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true }).First.ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex($"http://localhost:5173/estate-details/.*"));
    }

    [Test]
    [Order(7)]
    public async Task UpdateEstate_ShouldRedirectOwnerToEstateDetailsPage_WhenButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni", Exact = true }).First.ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex($"http://localhost:5173/estate-page/{_estateId}"));
    }

    [Test]
    [Order(8)]
    public async Task DeleteEstate_ShouldCancelDeletion_WhenCancelButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Dialog, new() { Name = "Da li sigurno želite da obrišete nekretninu?" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#swal2-html-container")).ToContainTextAsync("Uz nekretninu će biti obrisani i sve objave vezane za nju!");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Dialog, new() { Name = "Da li sigurno želite da obrišete nekretninu?" })).Not.ToBeVisibleAsync();
    }

    [Test]
    [Order(9)]
    public async Task DeleteEstate_ShouldDeleteEstate_WhenConfirmButtonIsClicked()
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

        await PageWithSettings.Locator("#navbarResponsive").GetByRole(AriaRole.Link, new() { Name = "NEKRETNINE" }).ClickAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Dialog, new() { Name = "Da li sigurno želite da obrišete nekretninu?" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#swal2-html-container")).ToContainTextAsync("Uz nekretninu će biti obrisani i sve objave vezane za nju!");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Nekretnina uspešno obrisana.");
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