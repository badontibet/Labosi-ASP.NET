using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NasIndexer.Data;
using NasIndexer.Model;

namespace Labosi_ASP.NET.Tests
{
    public static class TestDataFactory
    {
        public static async Task<FileTag> CreateTagAsync(
            CustomWebApplicationFactory factory,
            string name,
            string? description = null,
            string? color = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            var tag = new FileTag
            {
                Name = name,
                Description = description ?? string.Empty,
                Color = color ?? string.Empty
            };

            dbContext.FileTags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<FileTag> CreateAssignedTagAsync(
            CustomWebApplicationFactory factory,
            string name)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

            var tag = new FileTag
            {
                Name = name,
                Description = "Assigned test tag",
                Color = "#AA5500"
            };

            var directory = new DirectoryItem
            {
                Name = $"dir-{Guid.NewGuid():N}",
                Path = $"/tests/{Guid.NewGuid():N}",
                CreatedDate = now,
                ModifiedDate = now
            };

            var file = new FileItem
            {
                Name = $"file-{Guid.NewGuid():N}.txt",
                Path = $"{directory.Path}/file.txt",
                Extension = ".txt",
                Size = 128,
                CreatedDate = now,
                ModifiedDate = now,
                Directory = directory
            };

            file.Tags.Add(tag);

            dbContext.DirectoryItems.Add(directory);
            dbContext.FileItems.Add(file);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<FileTag?> FindTagAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .FirstOrDefaultAsync(tag => tag.Id == id);
        }
    }
}
