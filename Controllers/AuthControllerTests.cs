using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Timebox.Controllers;
using TimeboxTask = Timebox.Models.Task;
using TimeboxUser = Timebox.Models.User;
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
        var newUser = new TimeboxUser
        {
            Username = "testuser",
            Email = "test@testdomain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")

        };

        var result = await _controller.Register(newUser);
        var createdUser = result.Value;

        Assert.That(createdUser != null);
        Assert.That(_context.Users.Count() == 1);
        Assert.That(createdUser.Username == newUser.Username);
    }
}
