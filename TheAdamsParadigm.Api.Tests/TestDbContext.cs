using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Tests;

// The EF in-memory provider can't map pgvector's Vector type; tests never touch
// knowledge chunks, so drop that one column and keep the rest of the real model.
public class TestDbContext : ApplicationDbContext
{
    public TestDbContext()
        : base(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<KnowledgeChunk>().Ignore(e => e.Embedding);
    }
}
