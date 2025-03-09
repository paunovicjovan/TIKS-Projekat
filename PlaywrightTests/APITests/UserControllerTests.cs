using Microsoft.Playwright;

namespace PlaywrightTests.APITests;

public class UserControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request = null;

    [SetUp]
    public async Task Setup()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
    }

    [Test]
    public async Task Register_ShouldRegisterUser_WhenUserIsValid()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = "Petar",
                Email = "petar@gmail.com",
                Password = "@Petar123",
                PhoneNumber = "065 123 1212"
            }
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Code: {response.Status} - {response.StatusText}");
        } 
        
        var user = await response.JsonAsync();
        
        if ((user?.TryGetProperty("id", out var id) ?? false) &&
            (user?.TryGetProperty("username", out var username) ?? false) &&
            (user?.TryGetProperty("email", out var email) ?? false) &&
            (user?.TryGetProperty("phoneNumber", out var phoneNumber) ?? false) &&
            (user?.TryGetProperty("token", out var token) ?? false) &&
            (user?.TryGetProperty("role", out var role) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(username.GetString(), Is.EqualTo("Petar"));
                Assert.That(email.GetString(), Is.EqualTo("petar@gmail.com"));
                Assert.That(phoneNumber.GetString(), Is.EqualTo("065 123 1212"));
                Assert.That(token.GetString(), Is.Not.Empty);
                Assert.That(role.GetString(), Is.EqualTo("User"));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci.");
        }
    }


    [TearDown]
    public async Task End()
    {
        if (_request is not null)
        {
            await _request.DisposeAsync();
            _request = null;
        }
    }
}