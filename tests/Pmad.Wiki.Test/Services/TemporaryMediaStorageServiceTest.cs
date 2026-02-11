using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class TemporaryMediaStorageServiceTest : IDisposable
{
    private readonly string _tempDirectory;
    private readonly WikiOptions _options;
    private readonly TemporaryMediaStorageService _service;

    public TemporaryMediaStorageServiceTest()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TemporaryMediaStorageServiceTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _options = new WikiOptions
        {
            RepositoryRoot = _tempDirectory,
            WikiRepositoryName = "wiki",
            BranchName = "main"
        };

        var optionsWrapper = Options.Create(_options);
        _service = new TemporaryMediaStorageService(optionsWrapper);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region StoreTemporaryMediaAsync Tests

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithValidFile_StoresFileAndReturnsId()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileName = "test.png";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, fileName, fileContent, CancellationToken.None);

        // Assert
        Assert.NotNull(temporaryId);
        Assert.NotEmpty(temporaryId);
        Assert.True(Guid.TryParse(temporaryId, out _));

        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);
        Assert.True(media.ContainsKey(temporaryId));
        Assert.Equal(fileName, media[temporaryId].OriginalFileName);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithValidFile_StoresContentCorrectly()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileName = "document.pdf";
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // PDF header

        // Act
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, fileName, fileContent, CancellationToken.None);

        // Assert
        var retrievedContent = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);
        Assert.NotNull(retrievedContent);
        Assert.Equal(fileContent, retrievedContent);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithMultipleFiles_StoresAllFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var file1 = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var file2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var file3 = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // Act
        var id1 = await _service.StoreTemporaryMediaAsync(user, "image1.png", file1, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "image2.jpg", file2, CancellationToken.None);
        var id3 = await _service.StoreTemporaryMediaAsync(user, "doc.pdf", file3, CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Equal(3, media.Count);
        Assert.Contains(id1, media.Keys);
        Assert.Contains(id2, media.Keys);
        Assert.Contains(id3, media.Keys);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithDifferentUsers_IsolatesStorage()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        var id1 = await _service.StoreTemporaryMediaAsync(user1, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user2, "file2.png", fileContent, CancellationToken.None);

        // Assert
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);

        Assert.Single(media1);
        Assert.Single(media2);
        Assert.Contains(id1, media1.Keys);
        Assert.DoesNotContain(id2, media1.Keys);
        Assert.Contains(id2, media2.Keys);
        Assert.DoesNotContain(id1, media2.Keys);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_CreatesUserDirectory()
    {
        // Arrange
        var user = CreateMockUser("newuser@example.com", "New User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Assert
        var tempRoot = Path.Combine(_tempDirectory, ".temp-media");
        Assert.True(Directory.Exists(tempRoot));

        var userDirs = Directory.GetDirectories(tempRoot);
        Assert.Single(userDirs);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_PreservesFileExtension()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "image.jpg", fileContent, CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var storedFile = media[temporaryId];
        Assert.EndsWith(".jpg", storedFile.FilePath);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var beforeStore = DateTimeOffset.UtcNow;

        // Act
        await Task.Delay(10); // Small delay to ensure timestamp difference
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);
        await Task.Delay(10);
        var afterStore = DateTimeOffset.UtcNow;

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var storedFile = media[temporaryId];
        Assert.True(storedFile.CreatedAt >= beforeStore);
        Assert.True(storedFile.CreatedAt <= afterStore);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithEmptyFile_StoresEmptyFile()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = Array.Empty<byte>();

        // Act
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "empty.txt", fileContent, CancellationToken.None);

        // Assert
        var retrievedContent = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);
        Assert.NotNull(retrievedContent);
        Assert.Empty(retrievedContent);
    }

    [Fact]
    public async Task StoreTemporaryMediaAsync_WithLargeFile_StoresCorrectly()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[1024 * 1024]; // 1MB file
        new Random().NextBytes(fileContent);

        // Act
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "large.bin", fileContent, CancellationToken.None);

        // Assert
        var retrievedContent = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);
        Assert.NotNull(retrievedContent);
        Assert.Equal(fileContent.Length, retrievedContent.Length);
        Assert.Equal(fileContent, retrievedContent);
    }

    #endregion

    #region GetTemporaryMediaAsync Tests

    [Fact]
    public async Task GetTemporaryMediaAsync_WithValidId_ReturnsContent()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Act
        var retrievedContent = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);

        // Assert
        Assert.NotNull(retrievedContent);
        Assert.Equal(fileContent, retrievedContent);
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var nonExistentId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _service.GetTemporaryMediaAsync(user, nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_WithDifferentUser_ReturnsNull()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user1, "test.png", fileContent, CancellationToken.None);

        // Act
        var result = await _service.GetTemporaryMediaAsync(user2, temporaryId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_InvalidId_ThrowsArgumentException()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var invalidId = "invalid-id-format";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetTemporaryMediaAsync(user, invalidId, CancellationToken.None));
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetTemporaryMediaAsync(user, "", CancellationToken.None));
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_AfterRestart_LoadsFromFileSystem()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Simulate service restart by creating a new instance
        var newService = new TemporaryMediaStorageService(Options.Create(_options));

        // Act
        var retrievedContent = await newService.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);

        // Assert
        Assert.NotNull(retrievedContent);
        Assert.Equal(fileContent, retrievedContent);
    }

    [Fact]
    public async Task GetTemporaryMediaAsync_WithDeletedFile_ReturnsNull()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Delete the physical file manually
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        File.Delete(media[temporaryId].FilePath);

        // Act
        var result = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserTemporaryMediaAsync Tests

    [Fact]
    public async Task GetUserTemporaryMediaAsync_WithNoFiles_ReturnsEmptyDictionary()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");

        // Act
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Assert
        Assert.NotNull(media);
        Assert.Empty(media);
    }

    [Fact]
    public async Task GetUserTemporaryMediaAsync_WithMultipleFiles_ReturnsAllFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var id1 = await _service.StoreTemporaryMediaAsync(user, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "file2.jpg", fileContent, CancellationToken.None);
        var id3 = await _service.StoreTemporaryMediaAsync(user, "file3.pdf", fileContent, CancellationToken.None);

        // Act
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Assert
        Assert.Equal(3, media.Count);
        Assert.Contains(id1, media.Keys);
        Assert.Contains(id2, media.Keys);
        Assert.Contains(id3, media.Keys);
    }

    [Fact]
    public async Task GetUserTemporaryMediaAsync_ContainsCorrectMetadata()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileName = "test-image.png";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, fileName, fileContent, CancellationToken.None);

        // Act
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Assert
        Assert.Single(media);
        var info = media[temporaryId];
        Assert.Equal(temporaryId, info.TemporaryId);
        Assert.Equal(fileName, info.OriginalFileName);
        Assert.NotNull(info.FilePath);
        Assert.True(File.Exists(info.FilePath));
        Assert.True(info.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetUserTemporaryMediaAsync_AfterRestart_LoadsFromFileSystem()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var id1 = await _service.StoreTemporaryMediaAsync(user, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "file2.jpg", fileContent, CancellationToken.None);

        // Simulate service restart
        var newService = new TemporaryMediaStorageService(Options.Create(_options));

        // Act
        var media = await newService.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Assert
        Assert.Equal(2, media.Count);
        Assert.Contains(id1, media.Keys);
        Assert.Contains(id2, media.Keys);
    }

    [Fact]
    public async Task GetUserTemporaryMediaAsync_WithDifferentUser_ReturnsOnlyUserFiles()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        await _service.StoreTemporaryMediaAsync(user1, "file1.png", fileContent, CancellationToken.None);
        await _service.StoreTemporaryMediaAsync(user1, "file2.png", fileContent, CancellationToken.None);
        await _service.StoreTemporaryMediaAsync(user2, "file3.png", fileContent, CancellationToken.None);

        // Act
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);

        // Assert
        Assert.Equal(2, media1.Count);
        Assert.Single(media2);
    }

    [Fact]
    public async Task GetUserTemporaryMediaAsync_IgnoresNonGuidFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var validId = await _service.StoreTemporaryMediaAsync(user, "valid.png", fileContent, CancellationToken.None);

        // Create a non-GUID file in the user's directory
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var userDir = Path.GetDirectoryName(media[validId].FilePath)!;
        File.WriteAllBytes(Path.Combine(userDir, "not-a-guid.txt"), fileContent);

        // Simulate service restart to force reload
        var newService = new TemporaryMediaStorageService(Options.Create(_options));

        // Act
        var reloadedMedia = await newService.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Assert
        Assert.Single(reloadedMedia);
        Assert.Contains(validId, reloadedMedia.Keys);
    }

    #endregion

    #region CleanupUserTemporaryMediaAsync Tests

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithValidIds_DeletesFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var id1 = await _service.StoreTemporaryMediaAsync(user, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "file2.png", fileContent, CancellationToken.None);
        var id3 = await _service.StoreTemporaryMediaAsync(user, "file3.png", fileContent, CancellationToken.None);

        // Act
        await _service.CleanupUserTemporaryMediaAsync(user, new[] { id1, id2 }, CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);
        Assert.Contains(id3, media.Keys);
        Assert.DoesNotContain(id1, media.Keys);
        Assert.DoesNotContain(id2, media.Keys);
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithSingleId_DeletesFile()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Act
        await _service.CleanupUserTemporaryMediaAsync(user, new[] { temporaryId }, CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Empty(media);
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var nonExistentId = Guid.NewGuid().ToString("N");

        // Act & Assert
        await _service.CleanupUserTemporaryMediaAsync(user, new[] { nonExistentId }, CancellationToken.None);
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_RemovesPhysicalFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        var mediaBeforeCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var filePath = mediaBeforeCleanup[temporaryId].FilePath;
        Assert.True(File.Exists(filePath));

        // Act
        await _service.CleanupUserTemporaryMediaAsync(user, new[] { temporaryId }, CancellationToken.None);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithEmptyArray_DoesNothing()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);

        // Act
        await _service.CleanupUserTemporaryMediaAsync(user, Array.Empty<string>(), CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);
        Assert.Contains(temporaryId, media.Keys);
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var invalidId = "invalid-id";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CleanupUserTemporaryMediaAsync(user, new[] { invalidId }, CancellationToken.None));
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithMixedValidAndInvalidIds_ThrowsBeforeProcessing()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var validId = await _service.StoreTemporaryMediaAsync(user, "test.png", fileContent, CancellationToken.None);
        var invalidId = "invalid-id";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CleanupUserTemporaryMediaAsync(user, new[] { validId, invalidId }, CancellationToken.None));

        // Verify that the valid file was not deleted (validation happens before processing)
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);
        Assert.Contains(validId, media.Keys);
    }

    [Fact]
    public async Task CleanupUserTemporaryMediaAsync_WithDifferentUser_DoesNotDeleteOtherUserFiles()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var id1 = await _service.StoreTemporaryMediaAsync(user1, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user2, "file2.png", fileContent, CancellationToken.None);

        // Act - Try to cleanup user1's file using user2's context
        await _service.CleanupUserTemporaryMediaAsync(user2, new[] { id1 }, CancellationToken.None);

        // Assert - User1's file should still exist
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);

        Assert.Single(media1);
        Assert.Contains(id1, media1.Keys);
        Assert.Single(media2);
        Assert.Contains(id2, media2.Keys);
    }

    #endregion

    #region CleanupOldTemporaryMediaAsync Tests

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithOldFiles_DeletesThem()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var oldFileId = await _service.StoreTemporaryMediaAsync(user, "old.png", fileContent, CancellationToken.None);

        // Get the file path and modify its last write time to be very old
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var filePath = media[oldFileId].FilePath;
        var oldTime = DateTime.UtcNow.AddDays(-2);
        
        // Close any handles and set the time
        File.SetLastWriteTimeUtc(filePath, oldTime);
        File.SetCreationTimeUtc(filePath, oldTime);

        // Force file system flush by opening and closing
        using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // Just opening and closing should flush metadata
        }

        // Verify the time was set correctly
        var actualTime = File.GetLastWriteTimeUtc(filePath);
        Assert.True(actualTime < DateTime.UtcNow.AddHours(-47), $"File time not set correctly: {actualTime}, expected before {DateTime.UtcNow.AddHours(-47)}");

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);

        // Assert - File should be physically deleted
        await Task.Delay(50); // Small delay for file system
        Assert.False(File.Exists(filePath), "File should have been deleted");
        
        // Now check the cache/service state
        var mediaAfterCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Empty(mediaAfterCleanup);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithRecentFiles_KeepsThem()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var recentFileId = await _service.StoreTemporaryMediaAsync(user, "recent.png", fileContent, CancellationToken.None);

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);
        Assert.Contains(recentFileId, media.Keys);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithMixedOldAndRecentFiles_DeletesOnlyOld()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var oldFileId = await _service.StoreTemporaryMediaAsync(user, "old.png", fileContent, CancellationToken.None);
        var recentFileId = await _service.StoreTemporaryMediaAsync(user, "recent.png", fileContent, CancellationToken.None);

        // Make one file very old
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var oldTime = DateTime.UtcNow.AddDays(-2);
        File.SetLastWriteTimeUtc(media[oldFileId].FilePath, oldTime);
        File.SetCreationTimeUtc(media[oldFileId].FilePath, oldTime);

        // Ensure file system has flushed
        await Task.Delay(200);

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);

        // Small delay to ensure deletion completes
        await Task.Delay(100);

        // Assert
        Assert.False(File.Exists(media[oldFileId].FilePath), "Old file should have been deleted");
        Assert.True(File.Exists(media[recentFileId].FilePath), "Recent file should still exist");
        
        var mediaAfterCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(mediaAfterCleanup);
        Assert.Contains(recentFileId, mediaAfterCleanup.Keys);
        Assert.DoesNotContain(oldFileId, mediaAfterCleanup.Keys);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithMultipleUsers_CleansAllOldFiles()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var oldFile1 = await _service.StoreTemporaryMediaAsync(user1, "old1.png", fileContent, CancellationToken.None);
        var oldFile2 = await _service.StoreTemporaryMediaAsync(user2, "old2.png", fileContent, CancellationToken.None);
        var recentFile1 = await _service.StoreTemporaryMediaAsync(user1, "recent1.png", fileContent, CancellationToken.None);

        // Make some files old
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);
        File.SetLastWriteTimeUtc(media1[oldFile1].FilePath, DateTime.UtcNow.AddHours(-25));
        File.SetLastWriteTimeUtc(media2[oldFile2].FilePath, DateTime.UtcNow.AddHours(-25));

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);

        // Assert
        var media1After = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2After = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);

        Assert.Single(media1After);
        Assert.Contains(recentFile1, media1After.Keys);
        Assert.Empty(media2After);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithNoTempDirectory_DoesNotThrow()
    {
        // Arrange
        var newTempDir = Path.Combine(Path.GetTempPath(), "NoTempDir_" + Guid.NewGuid().ToString("N"));
        var options = new WikiOptions { RepositoryRoot = newTempDir };
        var service = new TemporaryMediaStorageService(Options.Create(options));

        // Act & Assert - Should not throw even if temp directory doesn't exist
        await service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithEmptyTempDirectory_DoesNotThrow()
    {
        // Arrange
        var emptyTempDir = Path.Combine(Path.GetTempPath(), "EmptyTempDir_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyTempDir);
        var tempMediaDir = Path.Combine(emptyTempDir, ".temp-media");
        Directory.CreateDirectory(tempMediaDir);

        var options = new WikiOptions { RepositoryRoot = emptyTempDir };
        var service = new TemporaryMediaStorageService(Options.Create(options));

        try
        {
            // Act & Assert
            await service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);
        }
        finally
        {
            Directory.Delete(emptyTempDir, true);
        }
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_UpdatesCache()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var oldFileId = await _service.StoreTemporaryMediaAsync(user, "old.png", fileContent, CancellationToken.None);

        // Load into cache
        await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);

        // Make file very old
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var oldTime = DateTime.UtcNow.AddDays(-2);
        File.SetLastWriteTimeUtc(media[oldFileId].FilePath, oldTime);
        File.SetCreationTimeUtc(media[oldFileId].FilePath, oldTime);

        // Ensure file system has flushed
        await Task.Delay(200);

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromHours(24), CancellationToken.None);

        // Small delay to ensure deletion completes
        await Task.Delay(100);

        // Assert - File should be deleted
        Assert.False(File.Exists(media[oldFileId].FilePath), "File should have been deleted");
        
        // Cache should be updated
        var mediaAfterCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Empty(mediaAfterCleanup);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithZeroTimeSpan_DeletesAllFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var id1 = await _service.StoreTemporaryMediaAsync(user, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "file2.png", fileContent, CancellationToken.None);

        // Set file times to be in the past
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        var pastTime = DateTime.UtcNow.AddSeconds(-2);
        File.SetLastWriteTimeUtc(media[id1].FilePath, pastTime);
        File.SetCreationTimeUtc(media[id1].FilePath, pastTime);
        File.SetLastWriteTimeUtc(media[id2].FilePath, pastTime);
        File.SetCreationTimeUtc(media[id2].FilePath, pastTime);

        // Ensure time has passed and file system has flushed
        await Task.Delay(200);

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.Zero, CancellationToken.None);

        // Small delay to ensure deletion completes
        await Task.Delay(100);

        // Assert
        Assert.False(File.Exists(media[id1].FilePath), "File 1 should have been deleted");
        Assert.False(File.Exists(media[id2].FilePath), "File 2 should have been deleted");
        
        var mediaAfterCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Empty(mediaAfterCleanup);
    }

    [Fact]
    public async Task CleanupOldTemporaryMediaAsync_WithVeryLargeTimeSpan_KeepsAllFiles()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var id1 = await _service.StoreTemporaryMediaAsync(user, "file1.png", fileContent, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "file2.png", fileContent, CancellationToken.None);

        // Act
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromDays(365), CancellationToken.None);

        // Assert
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Equal(2, media.Count);
        Assert.Contains(id1, media.Keys);
        Assert.Contains(id2, media.Keys);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_StoreRetrieveCleanup()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act 1: Store
        var temporaryId = await _service.StoreTemporaryMediaAsync(user, "workflow.png", fileContent, CancellationToken.None);
        Assert.NotNull(temporaryId);

        // Act 2: Retrieve
        var retrievedContent = await _service.GetTemporaryMediaAsync(user, temporaryId, CancellationToken.None);
        Assert.Equal(fileContent, retrievedContent);

        // Act 3: List
        var media = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Single(media);

        // Act 4: Cleanup
        await _service.CleanupUserTemporaryMediaAsync(user, new[] { temporaryId }, CancellationToken.None);

        // Assert
        var mediaAfterCleanup = await _service.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Empty(mediaAfterCleanup);
    }

    [Fact]
    public async Task MultiUserScenario_IsolationAndCleanup()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var user3 = CreateMockUser("user3@example.com", "User 3");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act: Multiple users store files
        var id1 = await _service.StoreTemporaryMediaAsync(user1, "file1.png", fileContent, CancellationToken.None);
        var id2a = await _service.StoreTemporaryMediaAsync(user2, "file2a.png", fileContent, CancellationToken.None);
        var id2b = await _service.StoreTemporaryMediaAsync(user2, "file2b.png", fileContent, CancellationToken.None);
        var id3 = await _service.StoreTemporaryMediaAsync(user3, "file3.png", fileContent, CancellationToken.None);

        // Assert: Each user sees only their files
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);
        var media3 = await _service.GetUserTemporaryMediaAsync(user3, CancellationToken.None);

        Assert.Single(media1);
        Assert.Equal(2, media2.Count);
        Assert.Single(media3);

        // Act: User2 cleans up one file
        await _service.CleanupUserTemporaryMediaAsync(user2, new[] { id2a }, CancellationToken.None);

        // Assert: Other users unaffected
        media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);
        media3 = await _service.GetUserTemporaryMediaAsync(user3, CancellationToken.None);

        Assert.Single(media1);
        Assert.Single(media2);
        Assert.Single(media3);
        Assert.Contains(id2b, media2.Keys);
        Assert.DoesNotContain(id2a, media2.Keys);
    }

    [Fact]
    public async Task ServiceRestart_PersistsData()
    {
        // Arrange
        var user = CreateMockUser("user@example.com", "Test User");
        var fileContent1 = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var fileContent2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act: Store with first service instance
        var id1 = await _service.StoreTemporaryMediaAsync(user, "persist1.png", fileContent1, CancellationToken.None);
        var id2 = await _service.StoreTemporaryMediaAsync(user, "persist2.jpg", fileContent2, CancellationToken.None);

        // Create new service instance (simulating restart)
        var newService = new TemporaryMediaStorageService(Options.Create(_options));

        // Assert: Data is still accessible
        var media = await newService.GetUserTemporaryMediaAsync(user, CancellationToken.None);
        Assert.Equal(2, media.Count);

        var content1 = await newService.GetTemporaryMediaAsync(user, id1, CancellationToken.None);
        var content2 = await newService.GetTemporaryMediaAsync(user, id2, CancellationToken.None);

        Assert.Equal(fileContent1, content1);
        Assert.Equal(fileContent2, content2);
    }

    [Fact]
    public async Task CleanupOldFiles_PreservesRecentFiles_DeletesOldFiles()
    {
        // Arrange
        var user1 = CreateMockUser("user1@example.com", "User 1");
        var user2 = CreateMockUser("user2@example.com", "User 2");
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Store files for different users
        var oldId1 = await _service.StoreTemporaryMediaAsync(user1, "old1.png", fileContent, CancellationToken.None);
        var recentId1 = await _service.StoreTemporaryMediaAsync(user1, "recent1.png", fileContent, CancellationToken.None);
        var oldId2 = await _service.StoreTemporaryMediaAsync(user2, "old2.png", fileContent, CancellationToken.None);
        var recentId2 = await _service.StoreTemporaryMediaAsync(user2, "recent2.png", fileContent, CancellationToken.None);

        // Make some files old
        var media1 = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2 = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);
        File.SetLastWriteTimeUtc(media1[oldId1].FilePath, DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(media2[oldId2].FilePath, DateTime.UtcNow.AddDays(-2));

        // Act: Cleanup files older than 1 day
        await _service.CleanupOldTemporaryMediaAsync(TimeSpan.FromDays(1), CancellationToken.None);

        // Assert
        var media1After = await _service.GetUserTemporaryMediaAsync(user1, CancellationToken.None);
        var media2After = await _service.GetUserTemporaryMediaAsync(user2, CancellationToken.None);

        Assert.Single(media1After);
        Assert.Contains(recentId1, media1After.Keys);
        Assert.DoesNotContain(oldId1, media1After.Keys);

        Assert.Single(media2After);
        Assert.Contains(recentId2, media2After.Keys);
        Assert.DoesNotContain(oldId2, media2After.Keys);
    }

    #endregion

    #region Helper Methods

    private static IWikiUser CreateMockUser(string email, string name)
    {
        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.GitEmail).Returns(email);
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.DisplayName).Returns(name);
        return mockUser.Object;
    }

    #endregion
}
