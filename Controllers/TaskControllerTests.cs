using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Timebox.Controllers;
using TimeboxTask = Timebox.Models.Task;
using TimeboxUser = Timebox.Models.User;
using Timebox.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Task = System.Threading.Tasks.Task;

[TestFixture]
public class TaskControllerTests
{
    private AppDbContext _context;
    private TaskController _controller;
    private TimeboxUser _testUser;
    private TimeboxUser _otherUser;
    [SetUp]
    public void Setup()
    {

        // TODO: test postgres database connection?	
        var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase("TestDb")
                                                    .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _controller = new TaskController(_context);
        _testUser = new TimeboxUser
        {
            Username = "testuser",
            Email = "test@testdomain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };


        var claims = new List<Claim>
        {
                                new Claim(ClaimTypes.NameIdentifier, _testUser.Id)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _otherUser = new TimeboxUser
        {
            Username = "otheruser",
            Email = "otheruser@testdomain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password456")
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        //_controller.Dispose();
    }


    [Test]
    public async Task TaskController_GetTasks_ReturnsAllTasks()
    {
        //ARRANGE
        _context.Tasks.Add(new TimeboxTask
        {
            Description = "Test Task 1",
            Duration = TimeSpan.FromHours(1).Minutes,
            UserId = _testUser.Id
        });

        await _context.SaveChangesAsync();
        //ACT
        var result = await _controller.GetAllTasks();
        var okResult = result.Value;
        //ASSERT
        Assert.That(okResult != null);
        Assert.That(okResult.Count() == 1);
    }

    public async Task TaskController_GetTasks_ReturnsOnlyUserTasks()
    {
        _context.Tasks.Add(new TimeboxTask
        {
            Description = "Test Task 1",
            Duration = TimeSpan.FromHours(1).Minutes,
            UserId = _testUser.Id
        });

        _context.Tasks.Add(new TimeboxTask
        {
            Description = "Test Task 2",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _otherUser.Id
        });

        await _context.SaveChangesAsync();

        var result = await _controller.GetAllTasks();
        var okResult = result.Value;

        Assert.That(okResult != null);
        Assert.That(okResult.Count() == 1);
        Assert.That(okResult.First().Description == "Test Task 1");
    }


    [Test]
    public async Task TaskController_GetTaskById_ReturnsTask()
    {
        _context.Tasks.Add(new TimeboxTask
        {
            Id = "1",
            Description = "Test Task 1",
            Duration = TimeSpan.FromHours(1).Minutes,
            UserId = _testUser.Id
        });

        await _context.SaveChangesAsync();

        var result = await _controller.GetTaskById("1");
        var okResult = result.Value;

        Assert.That(okResult != null);
    }

    [Test]
    public async Task TaskController_GetTaskById_ReturnsNotFound()
    {
        var result = await _controller.GetTaskById("none");
        var notFound = result.Result as NotFoundResult;
        Assert.That(notFound != null);
    }
    [Test]
    public async Task TaskController_GetTaskById_ReturnsNotFound_WhenAccessingOtherUserTask()
    {
        _context.Tasks.Add(new TimeboxTask
        {
            Id = "1",
            Description = "Should Not Be Found",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _otherUser.Id
        });

        await _context.SaveChangesAsync();

        var result = await _controller.GetTaskById("1");
        var notFound = result.Result as NotFoundResult;
        Assert.That(notFound != null);
    }

    [Test]
    public async Task TaskController_CreateTask_ReturnsCreatedTask()
    {
        TaskCreateDto task = new TaskCreateDto
        {
            Description = "New Task",
            Duration = TimeSpan.FromHours(2).Minutes
        };

        var result = await _controller.CreateTask(task);
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult != null);
        var createdTask = createdResult.Value as TimeboxTask;
        Assert.That(createdTask != null);
        Assert.That(createdTask.Description == task.Description);
    }

    [Test]
    public async Task TaskController_UpdateTask_ReturnsNoContent()
    {
        DateTime now = DateTime.UtcNow;
        TimeboxTask task = new TimeboxTask
        {
            Id = "1",
            Description = "New Task",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _testUser.Id
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        TaskUpdateDto updatedTask = new TaskUpdateDto
        {
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
            StartedAt = now
        };

        var result = await _controller.UpdateTask("1", updatedTask);
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult != null);
        var taskInDb = await _context.Tasks.FindAsync("1");
        Assert.That(taskInDb.Description == "Updated Task");
        Assert.That(taskInDb.Duration == TimeSpan.FromHours(3).Minutes);
        Assert.That(taskInDb.StartedAt != null);
        Assert.That(taskInDb.StartedAt.Value >= now);
    }

    [Test]
    public async Task TaskController_UpdateTask_ReturnsNotFound()
    {
        TaskUpdateDto updatedTask = new TaskUpdateDto
        {
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes
        };

        var result = await _controller.UpdateTask("1", updatedTask);

        var notFoundResult = result as NotFoundResult;
        Assert.That(notFoundResult != null);
    }

    [Test]
    public async Task TaskController_UpdateTask_ReturnsNotFound_WhenDeletedDuringUpdate()
    {
        TimeboxTask task = new TimeboxTask
        {
            Id = "1",
            Description = "New Task",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _testUser.Id
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        TaskUpdateDto updatedTask = new TaskUpdateDto
        {
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
        };

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateTask("1", updatedTask);
        var notFoundResult = result as NotFoundResult;
        Assert.That(notFoundResult != null);
    }
    [Test]
    public async Task TaskController_UpdateTask_ReturnsNotFound_WhenAccessingOtherUserTask()
    {
        _context.Tasks.Add(new TimeboxTask
        {
            Id = "1",
            Description = "Should Not Be Found",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _otherUser.Id
        });

        await _context.SaveChangesAsync();

        TaskUpdateDto updatedTask = new TaskUpdateDto
        {
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
        };

        var result = await _controller.UpdateTask("1", updatedTask);
        var notFound = result as NotFoundResult;
        Assert.That(notFound != null);
    }

    [Test]
    public async Task TaskController_DeleteTask_ReturnsNoContent()
    {
        TimeboxTask task = new TimeboxTask
        {
            Id = "1",
            Description = "Task to Delete",
            Duration = TimeSpan.FromHours(1).Minutes,
            UserId = _testUser.Id
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteTask("1");
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult != null);
    }

    [Test]
    public async Task TaskController_DeleteTask_ReturnsNotFound()
    {
        var result = await _controller.DeleteTask("1");
        var notFoundResult = result as NotFoundResult;
        Assert.That(notFoundResult != null);
    }

    [Test]
    public async Task TaskController_DeleteTask_ReturnsNotFound_WhenAccessingOtherUserTask()
    {
        _context.Tasks.Add(new TimeboxTask
        {
            Id = "1",
            Description = "Should Not Be Found",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _otherUser.Id
        });

        await _context.SaveChangesAsync();

        var result = await _controller.DeleteTask("1");
        var notFound = result as NotFoundResult;
        Assert.That(notFound != null);
    }
}

