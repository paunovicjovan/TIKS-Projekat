using Microsoft.Extensions.DependencyInjection;

namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    private UserService _userService;
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Register_ValidUser_RegistersUser()
    {
        Assert.Pass();
    }
}