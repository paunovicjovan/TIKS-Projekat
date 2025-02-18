using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Identity;
using DataLayer.Models;
using MongoDB.Driver;
using DataLayer.DTOs.User;

namespace NUnitTests;

[TestFixture]
public class UserServiceTests
{
    private UserService _userService;
    private TokenService _fakeTokenService;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<TokenService>> _mockLogger;
    private Mock<IPasswordHasher<User>> _mockPasswordHasher;
    private Mock<IMongoCollection<User>> _mockUserCollection;


    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TokenService>>();
        _fakeTokenService = new TokenService(_mockLogger.Object);
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUserCollection = new Mock<IMongoCollection<User>>();
        _mockPasswordHasher = new Mock<IPasswordHasher<User>>();
        _userService = new UserService(_fakeTokenService, _mockServiceProvider.Object);
    }


    // [Test]
    // public async Task Register_ValidUser_RegistersUser()
    // {
    //     // Arrange
    //     var userDto = new CreateUserDTO
    //     {
    //         Username = "validUsername",
    //         Email = "user@example.com",
    //         PhoneNumber = "123456789",
    //         Password = "StrongPassword123"
    //     };

    //     _mockUserCollection
    //             .Setup(u => u.Find(It.IsAny<Func<User, bool>>()))
    //             .ReturnsAsync((User)null); // Pretpostavljamo da nema postojećeg korisnika

    //     _mockPasswordHasher
    //         .Setup(ph => ph.HashPassword(null!, userDto.Password))
    //         .Returns("hashedPassword");

    //     _mockUserCollection
    //         .Setup(u => u.InsertOneAsync(It.IsAny<User>()))
    //         .Returns(Task.CompletedTask);  // Simulacija uspešnog umetanja novog korisnika

    //     var token = "generatedToken";
    //     _fakeTokenService
    //         .Setup(t => t.CreateToken(It.IsAny<User>()))
    //         .Returns(token);

    //     // Act
    //     var result = await _userService.Register(userDto);

    //     // Assert
    //     Assert.IsInstanceOf<AuthResponseDTO>(result.Value);
    //     var authResponse = result.Value as AuthResponseDTO;
    //     Assert.AreEqual(userDto.Username, authResponse.Username);
    //     Assert.AreEqual(userDto.Email, authResponse.Email);
    //     Assert.AreEqual(userDto.PhoneNumber, authResponse.PhoneNumber);
    //     Assert.AreEqual("User", authResponse.Role.ToString());
    //     Assert.AreEqual(token, authResponse.Token);

    //     _mockUserCollection.Verify(u => u.InsertOneAsync(It.IsAny<User>()), Times.Once);
    //     _mockPasswordHasher.Verify(ph => ph.HashPassword(null!, userDto.Password), Times.Once);
    // }
}