using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Timebox.Controllers;
using TimeboxTask = Timebox.Models.Task;
using TimeboxUser = Timebox.Models.User;
using LoginRequest = Timebox.Models.LoginRequest;
using RegisterRequest = Timebox.Models.RegisterRequest;
using BCrypt.Net;

[TestFixture]
public class AuthControllerTests
{
    private AppDbContext _context;
    private AuthController _controller;
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase("TestDb")
                        .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _controller = new AuthController(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task AuthController_Register_ReturnsCreatedUser()
    {

        var registerRequest = new RegisterRequest
        {
            Email = "test@testdomain.com",
            Username = "testuser",
            Password = "password123"
        };

        var result = await _controller.Register(registerRequest);
        var newUser = _context.Users.FirstOrDefault(u => u.Email == registerRequest.Email);

        Assert.That(newUser != null);
        Assert.That(_context.Users.Count() == 1);
    }
    [Test]
    public async Task AuthController_Login_ReturnJWTToken()
    {

        var testUser = new TimeboxUser
        {
            Username = "testuser",
            Email = "test@testdomain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@testdomain.com",
            Password = "password123"
        };

        var result = await _controller.Login(loginRequest);
        var token = result.Value;
        Assert.That(token != null);
        Assert.That(token.Length > 0);

    }
}
