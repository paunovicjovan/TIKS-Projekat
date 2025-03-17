using System.Text.Json;

namespace PlaywrightTests.APITests;

[TestFixture]
public class CommentControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _postId = string.Empty;
    private string _commentId = string.Empty;
    private string _commentAuthorToken = string.Empty;

    [OneTimeSetUp]
    public async Task CreateTestUserAndPost()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
        };

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            throw new Exception("Greška u kontekstu.");
        }

        // kreiranje test korisnika ciji ce se token koristiti za kreiranje objave i komentara
        var response = await _request.PostAsync("User/Register", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Username = Guid.NewGuid().ToString("N"),
                Email = $"{Guid.NewGuid():N}@gmail.com",
                Password = "P@ssword123",
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
            _commentAuthorToken = token.GetString() ?? string.Empty;
        }
        
        // kreiranje test objave
        headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_commentAuthorToken}" }
        };

        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
        
        response = await _request.PostAsync("Post/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Post",
                Content = "Hello World!"
            }
        });
        
        var postResponse = await response.JsonAsync();
        
        if (postResponse?.TryGetProperty("id", out var postId) ?? false)
        {
            _postId = postId.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID posta.");
        }
        
        // kreiranje test komentara na objavu
    }

    [SetUp]
    public async Task SetUp()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_commentAuthorToken}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
        
        var response = await _request.PostAsync("Comment/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Content = "Hello World!",
                PostId = _postId
            }
        });
        
        var commentResponse = await response.JsonAsync();
        
        if (commentResponse?.TryGetProperty("id", out var commentId) ?? false)
        {
            _commentId = commentId.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID komentara.");
        }
    }

    [Test]
    public async Task CreateComment_ShouldCreateComment_WhenDataIsValid([Values(1, 50, 1000)] int length)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var commentContent = new string('a', length);
        
        var response = await _request.PostAsync("Comment/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = commentContent,
                PostId = _postId
            }
        });

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var authResponse = await response.JsonAsync();

        if ((authResponse?.TryGetProperty("id", out var id) ?? false) &&
            (authResponse?.TryGetProperty("content", out var content) ?? false) &&
            (authResponse?.TryGetProperty("createdAt", out var createdAt) ?? false) &&
            (authResponse?.TryGetProperty("author", out var author) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(content.GetString(), Is.EqualTo(commentContent));
                Assert.That(createdAt.GetDateTime(), Is.Not.EqualTo(default(DateTime)));
                Assert.That(author.ValueKind, Is.EqualTo(JsonValueKind.Object));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task CreateComment_ShouldReturnError_WhenTokenIsNotValid()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_commentAuthorToken} not-valid" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.PostAsync("Comment/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = "Some content",
                PostId = _postId
            }
        });
        
        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    [Test]
    public async Task CreateComment_ShouldReturnError_WhenContentIsInvalid([Values(0, 1001)] int length)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var commentContent = new string('a', length);
        
        var response = await _request.PostAsync("Comment/Create", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = commentContent,
                PostId = _postId
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        
        Assert.That(message, Is.EqualTo("Komentar mora sadržati između 1 i 1000 karaktera."));
    }

    [TearDown]
    public async Task TearDown()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_commentAuthorToken}" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");

        try
        {
            var deleteCommentResponse = await _request.DeleteAsync($"Comment/Delete/{_commentId}");
            if (deleteCommentResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju test komentara: {deleteCommentResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Došlo je do greške: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_commentAuthorToken}" }
        };
        
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        _request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:5244/api/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });

        if (_request is null)
            throw new Exception("Greška u kontekstu.");
            
        try
        {
            if (!string.IsNullOrEmpty(_postId))
            {
                var deletePostResponse = await _request.DeleteAsync($"Post/Delete/{_postId}");
                if (deletePostResponse.Status != 204)
                {
                    throw new Exception($"Greška pri brisanju test objave: {deletePostResponse.Status}");
                }
            }
            
            var deleteCommentAuthorResponse = await _request.DeleteAsync($"User/Delete");
            if (deleteCommentAuthorResponse.Status != 204)
            {
                throw new Exception($"Greška pri brisanju test korisnika: {deleteCommentAuthorResponse.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri brisanju podataka: {ex.Message}");
        }
        finally
        {
            await _request.DisposeAsync();
            _request = null;
        }     
    }
}