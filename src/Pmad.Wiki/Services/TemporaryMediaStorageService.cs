using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

/// <summary>
/// File system-based implementation of temporary media storage service.
/// Stores files in a temp directory organized by user ID.
/// </summary>
public sealed class TemporaryMediaStorageService : ITemporaryMediaStorageService
{
    private readonly WikiOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TemporaryMediaInfo>> _userMediaCache = new();

    public TemporaryMediaStorageService(IOptions<WikiOptions> options)
    {
        _options = options.Value;
    }

    private string GetTemporaryStorageRoot()
    {
        return Path.Combine(_options.RepositoryRoot, ".temp-media");
    }

    private (string,string) GetUserTemporaryDirectory(IWikiUser user)
    {
        var safeUserId = GetSafeFileName(user.GitEmail);
        return (Path.Combine(GetTemporaryStorageRoot(), safeUserId), safeUserId);
    }

    public async Task<string> StoreTemporaryMediaAsync(IWikiUser user, string fileName, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        var (userDir, cacheKey) = GetUserTemporaryDirectory(user);
        Directory.CreateDirectory(userDir);

        // Generate a unique ID for this file
        var temporaryId = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(fileName);
        var tempFileName = temporaryId + extension;
        var filePath = Path.Combine(userDir, tempFileName);

        await File.WriteAllBytesAsync(filePath, fileContent, cancellationToken);

        var mediaInfo = new TemporaryMediaInfo
        {
            TemporaryId = temporaryId,
            OriginalFileName = fileName,
            FilePath = filePath,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userCache = _userMediaCache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, TemporaryMediaInfo>());
        userCache[temporaryId] = mediaInfo;

        return temporaryId;
    }

    public async Task<byte[]?> GetTemporaryMediaAsync(IWikiUser user, string temporaryId, CancellationToken cancellationToken = default)
    {
        WikiInputValidator.ValidateTempMediaId(temporaryId);

        var (userDir, cacheKey) = GetUserTemporaryDirectory(user);

        if (_userMediaCache.TryGetValue(cacheKey, out var userCache) && 
            userCache.TryGetValue(temporaryId, out var mediaInfo))
        {
            if (File.Exists(mediaInfo.FilePath))
            {
                return await File.ReadAllBytesAsync(mediaInfo.FilePath, cancellationToken);
            }
        }

        // Try to locate file if not in cache
        if (!Directory.Exists(userDir))
        {
            return null;
        }

        var files = Directory.GetFiles(userDir, temporaryId + ".*");
        if (files.Length > 0 && File.Exists(files[0]))
        {
            var fileName = Path.GetFileName(files[0]);
            var foundMediaInfo = new TemporaryMediaInfo
            {
                TemporaryId = temporaryId,
                OriginalFileName = fileName,
                FilePath = files[0],
                CreatedAt = File.GetCreationTimeUtc(files[0])
            };

            var foundUserCache = _userMediaCache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, TemporaryMediaInfo>());
            foundUserCache[temporaryId] = foundMediaInfo;

            return await File.ReadAllBytesAsync(files[0], cancellationToken);
        }

        return null;
    }

    public Task<Dictionary<string, TemporaryMediaInfo>> GetUserTemporaryMediaAsync(IWikiUser user, CancellationToken cancellationToken = default)
    {
        var (userDir, cacheKey) = GetUserTemporaryDirectory(user);

        if (_userMediaCache.TryGetValue(cacheKey, out var userCache))
        {
            return Task.FromResult(new Dictionary<string, TemporaryMediaInfo>(userCache));
        }

        // Try to load from file system
        var result = new Dictionary<string, TemporaryMediaInfo>();

        if (Directory.Exists(userDir))
        {
            foreach (var file in Directory.GetFiles(userDir))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (Guid.TryParse(fileName, out _))
                {
                    var fileMediaInfo = new TemporaryMediaInfo
                    {
                        TemporaryId = fileName,
                        OriginalFileName = Path.GetFileName(file),
                        FilePath = file,
                        CreatedAt = File.GetCreationTimeUtc(file)
                    };
                    result[fileName] = fileMediaInfo;
                }
            }

            if (result.Count > 0)
            {
                var fileUserCache = _userMediaCache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, TemporaryMediaInfo>());
                foreach (var kvp in result)
                {
                    fileUserCache[kvp.Key] = kvp.Value;
                }
            }
        }

        return Task.FromResult(result);
    }

    public async Task CleanupUserTemporaryMediaAsync(IWikiUser user, string[] temporaryIds, CancellationToken cancellationToken = default)
    {
        foreach(var tempId in temporaryIds)
        {
            WikiInputValidator.ValidateTempMediaId(tempId);
        }

        // Ensure cache is loaded
        await GetUserTemporaryMediaAsync(user, cancellationToken).ConfigureAwait(false);
        
        var (_, cacheKey) = GetUserTemporaryDirectory(user);

        // Remove from cache and delete files
        if (_userMediaCache.TryGetValue(cacheKey, out var userCache))
        {
            foreach(var tempId in temporaryIds)
            {
                if (userCache.TryRemove(tempId, out var infos))
                {
                    try
                    {
                        File.Delete(infos.FilePath);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
    }

    public Task CleanupOldTemporaryMediaAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var root = GetTemporaryStorageRoot();

        if (!Directory.Exists(root))
        {
            return Task.CompletedTask;
        }

        var cutoffDate = DateTimeOffset.UtcNow - olderThan;

        foreach (var userDir in Directory.GetDirectories(root))
        {
            var cacheKey = Path.GetFileName(userDir);

            _userMediaCache.TryGetValue(cacheKey, out var userCache);

            foreach (var file in Directory.GetFiles(userDir))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (lastWrite < cutoffDate.UtcDateTime)
                    {
                        File.Delete(file);

                        if (userCache != null)
                        {
                            var tempId = Path.GetFileNameWithoutExtension(file);
                            userCache.TryRemove(tempId, out _);
                        }
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        return Task.CompletedTask;
    }

    private static string GetSafeFileName(string userGitEmail)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userGitEmail));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
