using System.Diagnostics;
using DocSetLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618

namespace DocSetLibrary.Data;

public class DocSetContext : DbContext
{
    public DbSet<SearchIndex> SearchIndices { get; set; }
    private string DbPath { get; }

    public DocSetContext()
    {
        Debug.Assert(DocSetConverterHelper.Configure != null, "DocSetConverterHelper.Configure != null");
        DbPath = DocSetConverterHelper.Configure.DbPath;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => new SearchIndexEntityTypeConfiguration().Configure(modelBuilder.Entity<SearchIndex>());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}

public class SearchIndexEntityTypeConfiguration : IEntityTypeConfiguration<SearchIndex>
{
    public void Configure(EntityTypeBuilder<SearchIndex> builder)
    {
        builder.ToTable("searchindex");
        builder.HasKey(si => si.Id);
        builder.HasIndex(si => new { si.Name, si.Type, si.Path }, "anchor");
        builder.Property(si => si.Name).IsRequired();
        builder.Property(si => si.Type).IsRequired();
        builder.Property(si => si.Path).IsRequired();
    }
}

public class SearchIndex
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Path { get; set; }
}
