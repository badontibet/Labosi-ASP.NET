using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NasIndexer.Data;
using NasIndexer.Dtos;
using NasIndexer.Model;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class FileAttachmentsApiTests
    {
        [Fact]
        public async Task GetAttachments_ForExistingFile_ReturnsOkAndCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, file.Id);

            var response = await client.GetAsync($"/api/files/{file.Id}/attachments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var attachments = await response.Content.ReadFromJsonAsync<List<FileAttachmentDto>>();
            Assert.NotNull(attachments);
            Assert.Contains(attachments, item => item.Id == attachment.Id);
        }

        [Fact]
        public async Task GetAttachments_ForMissingFile_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/files/999999/attachments");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostAttachment_WithValidFile_ReturnsCreatedAndStoresMetadata()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("report.txt", "text/plain", "attachment content"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileAttachmentDto>();
            Assert.NotNull(dto);
            Assert.Equal(file.Id, dto.FileItemId);
            Assert.Equal("report.txt", dto.OriginalFileName);
            Assert.Equal("text/plain", dto.ContentType);
            Assert.Equal("attachment content".Length, dto.FileSize);
            Assert.EndsWith(".txt", dto.StoredFileName);
            Assert.DoesNotContain(":", dto.RelativePath);

            var storedAttachment = await TestDataFactory.FindFileAttachmentAsync(factory, dto.Id);
            Assert.NotNull(storedAttachment);
            Assert.Equal(dto.RelativePath, storedAttachment.RelativePath);

            var logs = await GetChangeLogsForFileAsync(factory, file.Id);
            var log = Assert.Single(logs);
            Assert.Equal(ChangeType.Modified, log.ChangeType);
            Assert.Contains("Attachment uploaded", log.NewValue);
            Assert.Contains("report.txt", log.NewValue);
            Assert.DoesNotContain("attachment content", log.NewValue, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PostAttachment_WithValidFile_PhysicallyWritesFileToDisk()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("evidence.pdf", "application/pdf", "%PDF-test"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileAttachmentDto>();
            Assert.NotNull(dto);
            var physicalPath = Path.Combine(factory.AttachmentStorageRoot, dto.RelativePath);
            Assert.True(File.Exists(physicalPath));
        }

        [Fact]
        public async Task PostAttachment_ForMissingFile_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();

            var response = await client.PostAsync(
                "/api/files/999999/attachments",
                CreateMultipartContent("missing.txt", "text/plain", "content"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostAttachment_WithEmptyFile_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("empty.txt", "text/plain", string.Empty));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostAttachment_WithDisallowedExtension_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("unsafe.exe", "application/octet-stream", "content"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAttachment_ForExistingAttachment_RemovesMetadataAndPhysicalFile()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, file.Id);
            var physicalPath = Path.Combine(factory.AttachmentStorageRoot, attachment.RelativePath);
            Assert.True(File.Exists(physicalPath));

            var response = await client.DeleteAsync($"/api/files/{file.Id}/attachments/{attachment.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await TestDataFactory.FindFileAttachmentAsync(factory, attachment.Id));
            Assert.False(File.Exists(physicalPath));

            var logs = await GetChangeLogsForFileAsync(factory, file.Id);
            var log = Assert.Single(logs);
            Assert.Equal(ChangeType.Modified, log.ChangeType);
            Assert.Contains("Attachment deleted", log.NewValue);
            Assert.Contains("original.txt", log.OldValue);
        }

        [Fact]
        public async Task DeleteAttachment_ForMissingAttachment_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.DeleteAsync($"/api/files/{file.Id}/attachments/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAttachment_WithMismatchedFileItemId_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var firstFile = await TestDataFactory.CreateFileItemAsync(factory);
            var secondFile = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, firstFile.Id);

            var response = await client.DeleteAsync($"/api/files/{secondFile.Id}/attachments/{attachment.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(await TestDataFactory.FindFileAttachmentAsync(factory, attachment.Id));
        }

        [Fact]
        public async Task DeleteAttachment_WhenPhysicalFileIsMissing_RemovesMetadataAndSucceeds()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, file.Id, createPhysicalFile: false);

            var response = await client.DeleteAsync($"/api/files/{file.Id}/attachments/{attachment.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await TestDataFactory.FindFileAttachmentAsync(factory, attachment.Id));
        }

        [Fact]
        public async Task AnonymousGetAttachments_ReturnsUnauthorized()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.GetAsync($"/api/files/{file.Id}/attachments");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedUserGetAttachments_Succeeds()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.GetAsync($"/api/files/{file.Id}/attachments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedNonRoleUserPostAttachment_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("user.txt", "text/plain", "content"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ManagerPostAttachment_Succeeds()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.PostAsync(
                $"/api/files/{file.Id}/attachments",
                CreateMultipartContent("manager.txt", "text/plain", "content"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task ManagerDeleteAttachment_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, file.Id);

            var response = await client.DeleteAsync($"/api/files/{file.Id}/attachments/{attachment.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AdminDeleteAttachment_Succeeds()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var attachment = await TestDataFactory.CreateFileAttachmentAsync(factory, file.Id);

            var response = await client.DeleteAsync($"/api/files/{file.Id}/attachments/{attachment.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static MultipartFormDataContent CreateMultipartContent(
            string fileName,
            string contentType,
            string content)
        {
            var multipart = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipart.Add(fileContent, "file", fileName);
            return multipart;
        }

        private static async Task<List<FileChangeLog>> GetChangeLogsForFileAsync(CustomWebApplicationFactory factory, int fileId)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.FileChangeLogs
                .AsNoTracking()
                .Where(changeLog => changeLog.FileId == fileId)
                .OrderBy(changeLog => changeLog.Id)
                .ToListAsync();
        }
    }
}
