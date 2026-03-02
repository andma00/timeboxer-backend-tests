using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Timebox.Controllers;
using TimeboxTask = Timebox.Models.Task;
using TimeboxUser = Timebox.Models.User;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;

[TestFixture]
public class TaskControllerTests
{
    private AppDbContext _context;
    private TaskController _controller;
    private TimeboxUser _testUser;
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
    public async Task TaskController_CreateTask_ReturnsCreatedTask()
    {
        TimeboxTask task = new TimeboxTask
        {
            Description = "New Task",
            Duration = TimeSpan.FromHours(2).Minutes,
            UserId = _testUser.Id
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

        TimeboxTask updatedTask = new TimeboxTask
        {
            Id = "1",
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
            StartedAt = now,
            UserId = _testUser.Id
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
    public async Task TaskController_UpdateTask_ReturnsBadRequest()
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

        TimeboxTask updatedTask = new TimeboxTask
        {
            Id = "2",
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
            UserId = _testUser.Id
        };

        var result = await _controller.UpdateTask("1", updatedTask);
        var badRequestResult = result as BadRequestResult;
        Assert.That(badRequestResult != null);

    }
    [Test]
    public async Task TaskController_UpdateTask_ReturnsNotFound()
    {
        TimeboxTask updatedTask = new TimeboxTask
        {
            Id = "1",
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
            UserId = _testUser.Id
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

        TimeboxTask updatedTask = new TimeboxTask
        {
            Id = "1",
            Description = "Updated Task",
            Duration = TimeSpan.FromHours(3).Minutes,
            UserId = _testUser.Id
        };

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateTask("1", updatedTask);
        var notFoundResult = result as NotFoundResult;
        Assert.That(notFoundResult != null);
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
}

