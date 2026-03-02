using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

[TestFixture]
public class DatabaseTests
{
    [Test]
    public void DbContext_Instantiates_Successfully()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("TestDb")
                                .Options;

        using var context = new AppDbContext(options);
        Assert.IsNotNull(context);
    }
}

