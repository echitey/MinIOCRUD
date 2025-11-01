using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinIOCRUD.Data;
using MinIOCRUD.Models;
using MinIOCRUD.Services;
using MinIOCRUD.Tests.Shared;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinIOCRUD.Tests.Services
{
    /// <summary>
    /// Unit tests for FolderService.
    /// 
    /// </summary>
    public class FolderServiceTests : TestSetup
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IMinioService> _minioMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<FolderService>> _loggerMock;
        private readonly FolderService _service;

        public FolderServiceTests()
        {
            _dbContext = CreateDbContext();
            _minioMock = new Mock<IMinioService>();
            _loggerMock = new Mock<ILogger<FolderService>>();

            // Mock configuration for MinIO bucket
            var configMock = new Mock<IConfiguration>();
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.Setup(s => s.Value).Returns("files");
            configMock.Setup(c => c.GetSection("Minio:Bucket")).Returns(sectionMock.Object);
            configMock.Setup(c => c["Minio:Bucket"]).Returns("files");
            _configMock = configMock;

            // Initialize service with mocked dependencies
            _service = new FolderService(_dbContext, _minioMock.Object, _configMock.Object, _loggerMock.Object);
        }

        /// <summary>
        /// Ensures folder creation persists correctly to the database.
        /// </summary>
        [Fact]
        public async Task CreateFolder_ShouldAddToDatabase()
        {
            // Arrange
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = "Documents",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _service.CreateFolderAsync(folder);
            var folders = await _dbContext.Folders.ToListAsync();

            // Assert
            folders.Should().ContainSingle(f => f.Name == "Documents");
        }

        /// <summary>
        /// Validates that CreateFolderAsync adds and returns the new folder.
        /// </summary>
        [Fact]
        public async Task CreateFolderAsync_Should_Add_And_Return_Folder()
        {
            // Arrange
            var folder = new Folder { Id = Guid.NewGuid(), Name = "Test Folder" };

            // Act
            var result = await _service.CreateFolderAsync(folder);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(folder.Name, result.Name);
            Assert.Equal(1, await _dbContext.Folders.CountAsync());
        }

        /// <summary>
        /// Ensures GetFolderByIdAsync retrieves the correct folder.
        /// </summary>
        [Fact]
        public async Task GetFolderAsync_Should_Return_Folder()
        {
            // Arrange
            var folder = new Folder { Id = Guid.NewGuid(), Name = "Root Folder" };
            await _dbContext.Folders.AddAsync(folder);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetFolderByIdAsync(folder.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(folder.Id, result!.Id);
        }

        /// <summary>
        /// Validates breadcrumb generation up to the root folder.
        /// </summary>
        [Fact]
        public async Task GetFolderDtoWithBreadcrumbsAsync_Should_Return_Breadcrumbs_Up_To_Root()
        {
            // Arrange
            var root = new Folder { Id = Guid.NewGuid(), Name = "Root" };
            var sub = new Folder { Id = Guid.NewGuid(), Name = "Sub", Parent = root, ParentId = root.Id };
            await _dbContext.Folders.AddRangeAsync(root, sub);
            await _dbContext.SaveChangesAsync();

            // Act
            var dto = await _service.GetFolderDtoWithBreadcrumbsAsync(sub.Id);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Sub", dto!.Name);
            Assert.NotNull(dto.Breadcrumb);
            Assert.Collection(dto.Breadcrumb!,
                b => Assert.Equal("Root", b.Name),
                b => Assert.Equal("Sub", b.Name));
        }

        /// <summary>
        /// Ensures GetRootContentsAsync only returns top-level folders and their files.
        /// </summary>
        [Fact]
        public async Task GetRootFoldersAsync_Should_Return_Only_Root_Folders_And_Files()
        {
            // Arrange
            var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ParentId = null };
            var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ParentId = root.Id };
            await _dbContext.Folders.AddRangeAsync(root, child);
            await _dbContext.SaveChangesAsync();

            // Act
            var (folders, files) = await _service.GetRootContentsAsync();

            // Assert
            Assert.Single(folders);
            Assert.Equal(root.Name, folders[0].Name);
            Assert.Empty(files);
        }

        /// <summary>
        /// Ensures DeleteFolderAsync removes the folder, related files, and calls MinIO cleanup.
        /// </summary>
        [Fact]
        public async Task DeleteFolderAsync_Should_Remove_Folder_And_Files()
        {
            // Arrange
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = "DeleteMe",
                Files = new List<FileRecord>
                {
                    new FileRecord
                    {
                        Id = Guid.NewGuid(),
                        FileName = "file.txt",
                        ObjectKey = "file.txt"
                    }
                }
            };

            await _dbContext.Folders.AddAsync(folder);
            await _dbContext.SaveChangesAsync();

            _minioMock
                .Setup(m => m.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteFolderAsync(folder.Id);

            // Assert
            Assert.Empty(_dbContext.Folders);
            Assert.Empty(_dbContext.Files);
            _minioMock.Verify(m => m.DeleteObjectAsync("files", "file.txt", CancellationToken.None), Times.Once);
        }
    }
}
