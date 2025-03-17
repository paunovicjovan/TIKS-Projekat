using System.Text.Json;

namespace PlaywrightTests.APITests;

[TestFixture]
public class CommentControllerTests : PlaywrightTest
{
    private IAPIRequestContext? _request;
    private string _postId = string.Empty;
    private string _commentId = string.Empty;
    private string _commentAuthorToken = string.Empty;
    private List<string> _postIdsToDelete = [];
    private bool _isCommentAlreadyDeleted;

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
            _postIdsToDelete.Add(_postId);
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID posta.");
        }
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
        _isCommentAlreadyDeleted = false;
        
        if (commentResponse?.TryGetProperty("id", out var commentId) ?? false)
        {
            _commentId = commentId.GetString()!;
        }
        else
        {
            throw new Exception("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID komentara.");
        }
    }

    #region CreateComment

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

    #endregion

    #region GetCommentsForPost

    [TestCase(0, 2, 2, 3)]
    [TestCase(1, 2, 2, 3)]
    [TestCase(2, 2, 1, 3)]
    [TestCase(3, 2, 0, 3)]
    public async Task GetCommentsForPost_ShouldReturnCorrectPaginatedComments_WhenParamsAreValid(int skip, int limit,
        int expectedCount, int totalCount)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Post sa komentarima",
                Content = "Hello World!"
            }
        });

        await Expect(response).ToBeOKAsync();

        var postResponse = await response.JsonAsync();

        string postId;
        
        if (postResponse?.TryGetProperty("id", out var id) ?? false)
        {
            postId = id.GetString()!;
            _postIdsToDelete.Add(postId);
        }
        else
        {
            Assert.Fail("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID posta.");
            return;
        }

        for (int i = 0; i < totalCount; i++)
        {
            response = await _request.PostAsync("Comment/Create", new APIRequestContextOptions()
            {
                DataObject = new
                {
                    Content = $"Komentar {i + 1}",
                    PostId = postId
                }
            });

            if (response.Status != 200)
            {
                Assert.Fail("Greška pri kreiranju test komentara.");
                return;
            }
        }
        
        response = await _request.GetAsync($"Comment/GetCommentsForPost/{postId}?skip={skip}&limit={limit}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }
        
        var paginatedComments = await response.JsonAsync();
        
        if ((paginatedComments?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedComments?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(totalCount));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(expectedCount));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetCommentsForPost_ShouldReturnEmptyList_WhenNoCommentsExist()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.PostAsync("Post/Create", new APIRequestContextOptions
        {
            DataObject = new
            {
                Title = "Post sa komentarima",
                Content = "Hello World!"
            }
        });

        await Expect(response).ToBeOKAsync();

        var postResponse = await response.JsonAsync();

        string postId;
        
        if (postResponse?.TryGetProperty("id", out var id) ?? false)
        {
            postId = id.GetString()!;
            _postIdsToDelete.Add(postId);
        }
        else
        {
            Assert.Fail("Došlo je do greške pri kreiranju test podataka. Server nije vratio ID posta.");
            return;
        }
        
        response = await _request.GetAsync($"Comment/GetCommentsForPost/{postId}");

        await Expect(response).ToBeOKAsync();

        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }
        
        var paginatedComments = await response.JsonAsync();
        
        if ((paginatedComments?.TryGetProperty("totalLength", out var totalLength) ?? false) &&
            (paginatedComments?.TryGetProperty("data", out var data) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(totalLength.GetInt64(), Is.EqualTo(0));
                Assert.That(data.EnumerateArray().Count(), Is.EqualTo(0));
            });
        }
        else
        {
            Assert.Fail("Nisu pronađeni svi potrebni podaci u odgovoru.");
        }
    }

    [Test]
    public async Task GetCommentsForPost_ShouldReturnError_WhenExceptionOccurs()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string invalidObjectId = "invalid id";
        var response = await _request.GetAsync($"Comment/GetCommentsForPost/{invalidObjectId}");
        
        Assert.That(response.Status, Is.EqualTo(400));
        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Došlo je do greške prilikom preuzimanja komentara."));
    }

    #endregion
    
    #region UpdateComment

    [Test]
    public async Task UpdateComment_ShouldReturnUpdatedComment_WhenSuccessful()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        const string newCommentContent = "Novi sadrzaj";

        var response = await _request.PutAsync($"Comment/Update/{_commentId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = newCommentContent
            }
        });

        await Expect(response).ToBeOKAsync();
        
        if (response.Status != 200)
        {
            Assert.Fail($"Došlo je do greške: {response.Status} - {response.StatusText}");
        }

        var updatedComment = await response.JsonAsync();

        if ((updatedComment?.TryGetProperty("id", out var id) ?? false) &&
            (updatedComment?.TryGetProperty("content", out var content) ?? false) &&
            (updatedComment?.TryGetProperty("createdAt", out var createdAt) ?? false) &&
            (updatedComment?.TryGetProperty("author", out var author) ?? false))
        {
            Assert.Multiple(() =>
            {
                Assert.That(id.GetString(), Is.Not.Empty);
                Assert.That(content.GetString(), Is.EqualTo(newCommentContent));
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
    public async Task UpdateComment_ShouldReturnError_WhenContentIsInvalid([Values(0, 1001)] int contentLength)
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }

        string newCommentContent = new string('a', contentLength);

        var response = await _request.PutAsync($"Comment/Update/{_commentId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = newCommentContent
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();

        Assert.That(message, Is.EqualTo("Komentar mora sadržati između 1 i 1000 karaktera."));
    }

    [Test]
    public async Task UpdateComment_ShouldReturnError_WhenCommentNotFound()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        const string nonExistentCommentId = "67d884a54e2da5f583e623e3";
        
        var response = await _request.PutAsync($"Comment/Update/{nonExistentCommentId}", new APIRequestContextOptions()
        {
            DataObject = new
            {
                Content = "Novi sadrzaj"
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();

        Assert.That(message, Is.EqualTo("Komentar nije pronađen ili nije izvršena promena."));
    }

    #endregion
    
    #region DeleteComment

    [Test]
    public async Task DeleteComment_ShouldReturnTrue_WhenCommentIsDeletedSuccessfully()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        var response = await _request.DeleteAsync($"Comment/Delete/{_commentId}");

        Assert.That(response.Status, Is.EqualTo(204));
        Assert.That(response.StatusText, Is.EqualTo("No Content"));

        _isCommentAlreadyDeleted = true;
    }

    [Test]
    public async Task DeleteComment_ShouldReturnError_WhenCommentNotFound()
    {
        if (_request is null)
        {
            Assert.Fail("Greška u kontekstu.");
            return;
        }
        
        await _request.DeleteAsync($"Comment/Delete/{_commentId}");
        
        var response = await _request.DeleteAsync($"Comment/Delete/{_commentId}");
        Assert.That(response.Status, Is.EqualTo(400));

        var message = await response.TextAsync();
        Assert.That(message, Is.EqualTo("Komentar nije pronađen."));
        
        _isCommentAlreadyDeleted = true;
    }
    
    [Test]
    public async Task DeleteComment_ShouldReturnError_WhenTokenIsNotValid()
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

        var response = await _request.DeleteAsync($"Comment/Delete/{_commentId}");
        Assert.That(response.Status, Is.EqualTo(401));
        Assert.That(response.StatusText, Is.EqualTo("Unauthorized"));
    }

    #endregion

    [TearDown]
    public async Task TearDown()
    {
        if (_isCommentAlreadyDeleted)
            return;
        
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
            if (_postIdsToDelete.Any())
            {
                foreach (var postId in _postIdsToDelete)
                {
                    var deletePostResponse = await _request.DeleteAsync($"Post/Delete/{postId}");
                    if (deletePostResponse.Status != 204)
                    {
                        throw new Exception($"Greška pri brisanju test objave: {deletePostResponse.Status}");
                    }
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