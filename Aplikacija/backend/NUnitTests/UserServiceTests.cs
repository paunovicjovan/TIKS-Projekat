namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    [Test]
    //imenovanje: NazivMetodeKojaSeTestira_OcekivaniIshod_Uslov
    public async Task Register_RegistersUser_WhenUserIsValid()
    {
        // Arrange
        var mockUsersCollection = new Mock<IMongoCollection<User>>();
    
        mockUsersCollection
            .Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
            .Callback<User, InsertOneOptions?, CancellationToken>((user, _, _) =>
            {
                user.Id = "some-id";
            })
            .Returns(Task.CompletedTask);
        
        var mockTokenService = new Mock<ITokenService>();
    
        const string fakeJwt = "some-jwt";
        mockTokenService.Setup(service => service.CreateToken(It.IsAny<User>()))
            .Returns(fakeJwt);
        
        var mockServiceProvider = new Mock<IServiceProvider>();
    
        var userService =
            new UserService(mockUsersCollection.Object, mockTokenService.Object, mockServiceProvider.Object);
    
        var userDto = new CreateUserDTO()
        {
            Username = "Petar",
            Email = "petar@gmail.com",
            Password = "@Petar123",
            PhoneNumber = "065 123 1212"
        };
    
        // Act
        (bool isError, var result, ErrorMessage? error) = await userService.Register(userDto);
    
        // Assert
        Assert.That(isError, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo(userDto.Username));
        Assert.That(result.Email, Is.EqualTo(userDto.Email));
        Assert.That(result.PhoneNumber, Is.EqualTo(userDto.PhoneNumber));
        Assert.That(result.Role, Is.EqualTo(UserRole.User));
        Assert.That(result.Token, Is.EqualTo(fakeJwt));
    }
}