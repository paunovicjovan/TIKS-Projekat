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
                    Title = $"Naslov objave {i + 1}",
                    Content = $"Sadrzaj objave {i + 1}",
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
            Headless = true,
            // SlowMo = 2000
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
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email1);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username1.ToUpper() }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "MOJ PROFIL" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Podaci o korisniku" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_username1);
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_phoneNumber1);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Nekretnine korisnika" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })
            .First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First)
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni", Exact = true }).First)
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByTestId("add-to-favorite-btn")).Not.ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave korisnika" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })
            .First).ToBeVisibleAsync();
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
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email2);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Podaci o korisniku" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_username1);
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_phoneNumber1);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" })).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Nekretnine korisnika" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })
            .First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši", Exact = true }).First)
            .ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni", Exact = true }).First)
            .ToBeHiddenAsync();
        await Expect(PageWithSettings.GetByTestId("add-to-favorite-btn").First).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Heading, new() { Name = "Objave korisnika" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })
            .First).ToBeVisibleAsync();
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

        // prijava nalogom drugog korisnika kako bi dodao nekretninu prvog korisnika u omiljene
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email2);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        // dodavanje omiljene nekretnine i provera da li je dodata
        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await PageWithSettings.GetByTestId("add-to-favorite-btn").First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Nekretnina je dodata u omiljene!");
        await Expect(PageWithSettings.GetByTestId("remove-from-favorite-btn").First).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username2.ToUpper() }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "OMILJENE NEKRETNINE" }).ClickAsync();
        await Expect(PageWithSettings.Locator("h5")).ToContainTextAsync("Luksuzna vila 1");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("510000 €");

        // uklanjanje omiljene nekretnine i provera da li je uklonjena
        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await PageWithSettings.GetByTestId("remove-from-favorite-btn").First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Nekretnina je uklonjena iz omiljenih!");
        await Expect(PageWithSettings.GetByTestId("add-to-favorite-btn").First).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = _username2.ToUpper() }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "OMILJENE NEKRETNINE" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#root"))
            .ToContainTextAsync("Korisnik trenutno nema omiljenih nekretnina.");
    }

    [Test]
    [Order(4)]
    public async Task UpdateUser_ShouldDisplayErrorMessage_WhenSomeDataIsInvalid()
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

        // unos neispravnog korisnickog imena
        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" }).ClickAsync();
        await PageWithSettings.Locator("input[type=\"text\"]").FillAsync("neispravno ime");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj izmene" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync(
            "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i .");
        await Task.Delay(5000); // cekanje da se skloni error message

        // unos neispravnog broja telefona (prazan string)
        await PageWithSettings.Locator("input[type=\"text\"]").FillAsync("ispravno_ime");
        await PageWithSettings.Locator("input[type=\"tel\"]").FillAsync("");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj izmene" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Unesite broj telefona.");
    }

    [Test]
    [Order(5)]
    public async Task UpdateUser_ShouldExitEditMode_WhenCancelButtonIsClicked()
    {
        if (PageWithSettings is null)
        {
            Assert.Fail("Greška, stranica ne postoji.");
            return;
        }

        // login sa prvim korisnikom
        await PageWithSettings.GotoAsync("http://localhost:5173/login");
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite e-mail" }).FillAsync(_email1);
        await PageWithSettings.GetByRole(AriaRole.Textbox, new() { Name = "Unesite lozinku" }).FillAsync(_password);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Prijavite Se" }).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/");

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" }).ClickAsync();
        await PageWithSettings.Locator("input[type=\"text\"]").FillAsync("novo_ime");
        await PageWithSettings.Locator("input[type=\"tel\"]").FillAsync("065 231 41541");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" }).ClickAsync();

        // provera da podaci nisu promenjeni
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_username1);
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync(_phoneNumber1);
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeVisibleAsync();
    }

    [Test]
    [Order(6)]
    public async Task UpdateUser_ShouldChangeData_WhenDataIsValid()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user2Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni podatke" }).ClickAsync();
        await PageWithSettings.Locator("input[type=\"text\"]").FillAsync("novo_ime");
        await PageWithSettings.Locator("input[type=\"tel\"]").FillAsync("065 231 4545");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj izmene" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#dropdown-basic")).ToContainTextAsync("NOVO_IME");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("novo_ime");
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("065 231 4545");
    }

    [Test]
    [Order(7)]
    public async Task DeleteUser_ShouldCancelDeletion_WhenCancelButtonIsClicked()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user2Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Dialog, new() { Name = "Da li sigurno želite da obrišete" }))
            .ToBeVisibleAsync();
        await Expect(PageWithSettings.Locator("#swal2-title"))
            .ToContainTextAsync("Da li sigurno želite da obrišete nalog?");
        await Expect(PageWithSettings.Locator("#swal2-html-container"))
            .ToContainTextAsync("Ovime će biti obrisani svi vaši podaci, nekretnine, objave i komentari trajno.");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();

        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Dialog, new() { Name = "Da li sigurno želite da obrišete" }))
            .Not.ToBeVisibleAsync();
    }

    [Test]
    [Order(8)]
    public async Task DeleteUser_ShouldDeleteUser_WhenConfirmButtonIsClicked()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user2Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši nalog" }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Status)).ToContainTextAsync("Uspešno brisanje naloga.");
        await Expect(PageWithSettings).ToHaveURLAsync("http://localhost:5173/login");
    }

    [Test]
    [Order(9)]
    public async Task SeeEstateDetails_ShouldRedirectUserToEstatePage_WhenButtonIsClicked()
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
        
        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje" }).First.ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex("http://localhost:5173/estate-details/.*"));
    }

    [Test]
    [Order(10)]
    public async Task DeleteEstate_ShouldCancelDeleteEstate_WhenCancelButtonIsClicked()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).Nth(1).ClickAsync();
        await Expect(PageWithSettings.Locator("#swal2-title")).ToContainTextAsync("Da li sigurno želite da obrišete nekretninu?");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#root")).ToContainTextAsync("Luksuzna vila 1");
    }
    
    [Test]
    [Order(11)]
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).Nth(1).ClickAsync();
        await Expect(PageWithSettings.Locator("#swal2-title")).ToContainTextAsync("Da li sigurno želite da obrišete nekretninu?");
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Obriši" }).ClickAsync();
        await Expect(PageWithSettings.Locator("#root")).Not.ToContainTextAsync("Luksuzna vila 1");
    }

    [Test]
    [Order(12)]
    public async Task UpdateEstate_ShouldRedirectUserToEstatePage_WhenButtonIsClicked()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Izmeni" }).Nth(1).ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex("http://localhost:5173/estate-page/.*"));
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj" })).ToBeVisibleAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Otkaži" })).ToBeVisibleAsync();
    }

    [Test]
    [Order(13)]
    public async Task SeePostDetails_ShouldRedirectUserToPostPage_WhenButtonIsClicked()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true }).First.ClickAsync();
        await Expect(PageWithSettings).ToHaveURLAsync(new Regex("http://localhost:5173/forum/.*"));
    }
    
    [Test]
    [Order(14)]
    public async Task EstatePaginationChange_ShouldCountEstatesOnPage_WhenCountOfEstatesPerPageChange()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");

        // kreirano je 8 nekretnina, ali je jedna obrisana kroz test iznad, pa ih ima ukupno 7
        // provera za 10 nekretnina po stranici
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })).ToHaveCountAsync(7);

        // provera za 5 nekretnina po stranici
        await PageWithSettings.GetByText("10", new() { Exact = true }).First.ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "5" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })).ToHaveCountAsync(5);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).First.ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })).ToHaveCountAsync(2);

        // provera za 20 nekretnina po stranici
        await PageWithSettings.GetByText("5", new() { Exact = true }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "20" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj Detalje", Exact = true })).ToHaveCountAsync(7);
    }

    [Test]
    [Order(15)]
    public async Task PostPaginationChange_ShouldCountPostsOnPage_WhenCountOfPostsPerPageChange()
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

        await PageWithSettings.GotoAsync($"http://localhost:5173/user-profile/{_user1Id}");
        
        // provera za 10 objava po stranici
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })).ToHaveCountAsync(8);

        // provera za 5 objava po stranici
        await PageWithSettings.GetByText("10", new() { Exact = true }).Nth(1).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "5" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })).ToHaveCountAsync(5);
        await PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).Nth(1).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })).ToHaveCountAsync(3);

        // provera za 20 objava po stranici
        await PageWithSettings.GetByText("5", new() { Exact = true }).ClickAsync();
        await PageWithSettings.GetByRole(AriaRole.Option, new() { Name = "20" }).ClickAsync();
        await Expect(PageWithSettings.GetByRole(AriaRole.Button, new() { Name = "Pogledaj detalje", Exact = true })).ToHaveCountAsync(8);
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

        // drugi korisnik se brise kroz test za delete
    }
}