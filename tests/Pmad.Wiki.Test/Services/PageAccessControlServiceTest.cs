using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class PageAccessControlServiceTest
{
    private readonly Mock<IGitRepositoryService> _mockGitRepositoryService;
    private readonly Mock<IGitRepository> _mockRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly WikiOptions _options;
    private readonly PageAccessControlService _service;

    public PageAccessControlServiceTest()
    {
        _mockGitRepositoryService = new Mock<IGitRepositoryService>();
        _mockRepository = new Mock<IGitRepository>();
        _mockCache = new Mock<IMemoryCache>();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            UsePageLevelPermissions = true
        };

        var optionsWrapper = Options.Create(_options);

        _mockGitRepositoryService
            .Setup(x => x.GetRepositoryByPath(It.IsAny<string>()))
            .Returns(_mockRepository.Object);

        _service = new PageAccessControlService(
            _mockGitRepositoryService.Object,
            optionsWrapper,
            _mockCache.Object);
    }

    #region CheckPageAccessAsync Tests

    [Fact]
    public async Task CheckPageAccessAsync_WhenPermissionsDisabled_ReturnsFullAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        // Act
        var result = await _service.CheckPageAccessAsync("test/page", ["user"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Null(result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenNoRulesExist_ReturnsFullAccess()
    {
        // Arrange
        SetupNoPermissionsFile();

        // Act
        var result = await _service.CheckPageAccessAsync("test/page", ["user"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Null(result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenRuleMatchesAndUserInReadGroup_GrantsReadAccess()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/settings", ["admin"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Equal("admin/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenRuleMatchesButUserNotInReadGroup_DeniesReadAccess()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);

        // Assert
        Assert.False(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("admin/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenUserInReadButNotWriteGroup_GrantsReadOnlyAccess()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("docs/**", ["readers", "editors"], ["editors"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("docs/readme", ["readers"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("docs/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenUserInMultipleGroups_GrantsAccessIfAnyGroupMatches()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin", "superuser"], ["admin"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/settings", ["user", "superuser", "guest"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("admin/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenEmptyReadGroups_AllowsAllUsersToRead()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("public/**", [], ["editors"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("public/readme", ["user"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("public/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenEmptyWriteGroups_AllowsAllUsersToWrite()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("public/**", ["users"], [], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("public/readme", ["users"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Equal("public/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenEmptyBothGroups_AllowsFullAccess()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("open/**", [], [], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("open/page", [], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Equal("open/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WithMultipleRules_UsesFirstMatchingRule()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("admin/public/**", [], [], 1),
            new PageAccessRule("**", [], ["users"], 2)
        };
        SetupPermissionsFile(rules);

        // Act - Should match first rule
        var result = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);

        // Assert
        Assert.False(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("admin/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WithMultipleRules_RespectsOrder()
    {
        // Arrange - Lower order value takes precedence
        // The rules list is intentionally in "wrong" order to verify service sorts them
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("**", [], ["users"], 10),
            new PageAccessRule("admin/**", ["admin"], ["admin"], 1)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);

        // Assert - Should match order 1 rule (admin/**), not order 10 (**)
        // Even though ** matches everything and has empty read groups (allowing all),
        // admin/** is checked first because it has lower order value
        Assert.False(result.CanRead);
        Assert.False(result.CanEdit);
        Assert.Equal("admin/**", result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WhenNoRuleMatches_ReturnsFullAccess()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("docs/**", ["users"], ["editors"], 1)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("public/page", ["user"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
        Assert.Null(result.MatchedPattern);
    }

    [Fact]
    public async Task CheckPageAccessAsync_GroupComparison_IsCaseInsensitive()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["Admin"], ["ADMIN"], 0)
        };
        SetupPermissionsFile(rules);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/settings", ["admin"], CancellationToken.None);

        // Assert
        Assert.True(result.CanRead);
        Assert.True(result.CanEdit);
    }

    [Fact]
    public async Task CheckPageAccessAsync_WithEmptyUserGroups_OnlyMatchesEmptyGroupRules()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("public/**", [], [], 1)
        };
        SetupPermissionsFile(rules);

        // Act
        var adminResult = await _service.CheckPageAccessAsync("admin/settings", [], CancellationToken.None);
        var publicResult = await _service.CheckPageAccessAsync("public/page", [], CancellationToken.None);

        // Assert
        Assert.False(adminResult.CanRead);
        Assert.False(adminResult.CanEdit);

        Assert.True(publicResult.CanRead);
        Assert.True(publicResult.CanEdit);
    }

    #endregion

    #region GetRulesAsync Tests

    [Fact]
    public async Task GetRulesAsync_WhenPermissionsDisabled_ReturnsEmptyList()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public async Task GetRulesAsync_WhenNoFileExists_ReturnsEmptyList()
    {
        // Arrange
        SetupNoPermissionsFile();

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public async Task GetRulesAsync_WhenFileExists_ReturnsParseRules()
    {
        // Arrange
        var expectedRules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("docs/**", ["users"], ["editors"], 1)
        };
        SetupPermissionsFile(expectedRules);

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal("admin/**", rules[0].Pattern);
        Assert.Equal("docs/**", rules[1].Pattern);
    }

    [Fact]
    public async Task GetRulesAsync_UsesCacheWhenAvailable()
    {
        // Arrange
        var cachedRules = new List<PageAccessRule>
        {
            new PageAccessRule("cached/**", ["user"], ["user"], 0)
        };

        object cacheEntry = cachedRules;
        _mockCache
            .Setup(x => x.TryGetValue("WikiPageAccessRules", out cacheEntry))
            .Returns(true);

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        Assert.Single(rules);
        Assert.Equal("cached/**", rules[0].Pattern);

        // Verify repository was not accessed
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRulesAsync_CachesResultForSubsequentCalls()
    {
        // Arrange
        var expectedRules = new List<PageAccessRule>
        {
            new PageAccessRule("test/**", ["user"], ["user"], 0)
        };

        var content = "# Test Rules\ntest/** | user | user";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        object? cacheEntry = null;
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupProperty(x => x.Value);
        cacheEntryMock.SetupProperty(x => x.AbsoluteExpirationRelativeToNow);

        _mockCache
            .Setup(x => x.TryGetValue("WikiPageAccessRules", out cacheEntry))
            .Returns(false);

        _mockCache
            .Setup(x => x.CreateEntry("WikiPageAccessRules"))
            .Returns(cacheEntryMock.Object);

        _mockRepository
            .Setup(x => x.ReadFileAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        _mockCache.Verify(x => x.CreateEntry("WikiPageAccessRules"), Times.Once);
        Assert.Equal(TimeSpan.FromMinutes(15), cacheEntryMock.Object.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task GetRulesAsync_WhenFileNotFound_CachesEmptyList()
    {
        // Arrange
        object? cacheEntry = null;
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupProperty(x => x.Value);
        cacheEntryMock.SetupProperty(x => x.AbsoluteExpirationRelativeToNow);

        _mockCache
            .Setup(x => x.TryGetValue("WikiPageAccessRules", out cacheEntry))
            .Returns(false);

        _mockCache
            .Setup(x => x.CreateEntry("WikiPageAccessRules"))
            .Returns(cacheEntryMock.Object);

        _mockRepository
            .Setup(x => x.ReadFileAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var rules = await _service.GetRulesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(rules);
        _mockCache.Verify(x => x.CreateEntry("WikiPageAccessRules"), Times.Once);
    }

    #endregion

    #region SaveRulesAsync Tests

    [Fact]
    public async Task SaveRulesAsync_WhenFileDoesNotExist_AddsNewFile()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };

        var user = CreateMockUser("test@example.com", "Test User");

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        // Act
        await _service.SaveRulesAsync(rules, "Add permissions", user, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.CreateCommitAsync(
            "main",
            It.Is<IEnumerable<GitCommitOperation>>(ops => ops.Any(op => op is AddFileOperation)),
            It.Is<GitCommitMetadata>(m => m.Message == "Add permissions" && m.AuthorName == "Test User"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveRulesAsync_WhenFileExists_UpdatesFile()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };

        var user = CreateMockUser("test@example.com", "Test User");

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Blob);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        // Act
        await _service.SaveRulesAsync(rules, "Update permissions", user, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.CreateCommitAsync(
            "main",
            It.Is<IEnumerable<GitCommitOperation>>(ops => ops.Any(op => op is UpdateFileOperation)),
            It.Is<GitCommitMetadata>(m => m.Message == "Update permissions"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveRulesAsync_ClearsCache()
    {
        // Arrange
        var rules = new List<PageAccessRule>();
        var user = CreateMockUser("test@example.com", "Test User");

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Blob);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        // Act
        await _service.SaveRulesAsync(rules, "Clear rules", user, CancellationToken.None);

        // Assert
        _mockCache.Verify(x => x.Remove("WikiPageAccessRules"), Times.Once);
    }

    [Fact]
    public async Task SaveRulesAsync_SerializesRulesCorrectly()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("docs/**", ["users"], ["editors"], 1)
        };

        var user = CreateMockUser("test@example.com", "Test User");

        byte[]? capturedContent = null;

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (branch, ops, metadata, ct) =>
                {
                    var addOp = ops.OfType<AddFileOperation>().First();
                    capturedContent = addOp.Content;
                })
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        // Act
        await _service.SaveRulesAsync(rules, "Test save", user, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedContent);
        var content = Encoding.UTF8.GetString(capturedContent);
        Assert.Contains("admin/**", content);
        Assert.Contains("docs/**", content);
        Assert.Contains("admin | admin", content);
        Assert.Contains("users | editors", content);
    }

    [Fact]
    public async Task SaveRulesAsync_UsesCorrectRepositoryPath()
    {
        // Arrange
        var rules = new List<PageAccessRule>();
        var user = CreateMockUser("test@example.com", "Test User");

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        // Act
        await _service.SaveRulesAsync(rules, "Test", user, CancellationToken.None);

        // Assert
        _mockGitRepositoryService.Verify(
            x => x.GetRepositoryByPath(Path.Combine("/test/repos", "wiki")),
            Times.Once);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_RemovesCacheEntry()
    {
        // Act
        _service.ClearCache();

        // Assert
        _mockCache.Verify(x => x.Remove("WikiPageAccessRules"), Times.Once);
    }

    #endregion

    #region Integration-style Tests

    [Fact]
    public async Task CompleteWorkflow_AddRules_CheckAccess_UpdateRules()
    {
        // Arrange
        var user = CreateMockUser("admin@example.com", "Admin User");

        // Setup initial state - no rules
        SetupNoPermissionsFile();

        // Act 1: Check access with no rules
        var initialAccess = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);

        // Assert 1: Should have full access
        Assert.True(initialAccess.CanRead);
        Assert.True(initialAccess.CanEdit);

        // Act 2: Add rules
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        await _service.SaveRulesAsync(rules, "Add admin rules", user, CancellationToken.None);

        // Setup for next check - return the rules we just saved
        SetupPermissionsFile(rules);

        // Act 3: Check access after adding rules
        var restrictedAccess = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);

        // Assert 3: Should be restricted now
        Assert.False(restrictedAccess.CanRead);
        Assert.False(restrictedAccess.CanEdit);
        Assert.Equal("admin/**", restrictedAccess.MatchedPattern);
    }

    [Fact]
    public async Task RulePriority_SpecificBeforeGeneric()
    {
        // Arrange - More specific rules should take precedence
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/public/**", [], [], 0),        // Public admin pages
            new PageAccessRule("admin/**", ["admin"], ["admin"], 1),  // Other admin pages
            new PageAccessRule("**", [], ["users"], 2)                 // Everything else
        };
        SetupPermissionsFile(rules);

        // Act & Assert
        var publicAdminAccess = await _service.CheckPageAccessAsync("admin/public/readme", [], CancellationToken.None);
        Assert.True(publicAdminAccess.CanRead);
        Assert.True(publicAdminAccess.CanEdit);
        Assert.Equal("admin/public/**", publicAdminAccess.MatchedPattern);

        var restrictedAdminAccess = await _service.CheckPageAccessAsync("admin/settings", ["user"], CancellationToken.None);
        Assert.False(restrictedAdminAccess.CanRead);
        Assert.Equal("admin/**", restrictedAdminAccess.MatchedPattern);

        var generalAccess = await _service.CheckPageAccessAsync("docs/readme", ["users"], CancellationToken.None);
        Assert.True(generalAccess.CanRead);
        Assert.True(generalAccess.CanEdit);
        Assert.Equal("**", generalAccess.MatchedPattern);
    }

    [Fact]
    public async Task MultipleUserScenarios_DifferentAccessLevels()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("docs/**", ["readers", "editors", "admin"], ["editors", "admin"], 1),
            new PageAccessRule("public/**", [], [], 2)
        };
        SetupPermissionsFile(rules);

        // Act & Assert - Admin user
        var adminAccess = await _service.CheckPageAccessAsync("admin/settings", ["admin"], CancellationToken.None);
        Assert.True(adminAccess.CanRead);
        Assert.True(adminAccess.CanEdit);

        var adminDocsAccess = await _service.CheckPageAccessAsync("docs/guide", ["admin"], CancellationToken.None);
        Assert.True(adminDocsAccess.CanRead);
        Assert.True(adminDocsAccess.CanEdit);

        // Act & Assert - Editor user
        var editorAdminAccess = await _service.CheckPageAccessAsync("admin/settings", ["editors"], CancellationToken.None);
        Assert.False(editorAdminAccess.CanRead);
        Assert.False(editorAdminAccess.CanEdit);

        var editorDocsAccess = await _service.CheckPageAccessAsync("docs/guide", ["editors"], CancellationToken.None);
        Assert.True(editorDocsAccess.CanRead);
        Assert.True(editorDocsAccess.CanEdit);

        // Act & Assert - Reader user
        var readerDocsAccess = await _service.CheckPageAccessAsync("docs/guide", ["readers"], CancellationToken.None);
        Assert.True(readerDocsAccess.CanRead);
        Assert.False(readerDocsAccess.CanEdit);

        // Act & Assert - Anonymous user
        var anonPublicAccess = await _service.CheckPageAccessAsync("public/readme", [], CancellationToken.None);
        Assert.True(anonPublicAccess.CanRead);
        Assert.True(anonPublicAccess.CanEdit);

        var anonDocsAccess = await _service.CheckPageAccessAsync("docs/guide", [], CancellationToken.None);
        Assert.False(anonDocsAccess.CanRead);
        Assert.False(anonDocsAccess.CanEdit);
    }

    #endregion

    #region Helper Methods

    private void SetupNoPermissionsFile()
    {
        object? cacheEntry = null;
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupProperty(x => x.Value);
        cacheEntryMock.SetupProperty(x => x.AbsoluteExpirationRelativeToNow);

        _mockCache
            .Setup(x => x.TryGetValue("WikiPageAccessRules", out cacheEntry))
            .Returns(false);

        _mockCache
            .Setup(x => x.CreateEntry("WikiPageAccessRules"))
            .Returns(cacheEntryMock.Object);

        _mockRepository
            .Setup(x => x.ReadFileAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());
    }

    private void SetupPermissionsFile(List<PageAccessRule> rules)
    {
        var serializedContent = AccessControlRuleSerializer.SerializeRules(rules);
        var contentBytes = Encoding.UTF8.GetBytes(serializedContent);

        object? cacheEntry = null;
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupProperty(x => x.Value);
        cacheEntryMock.SetupProperty(x => x.AbsoluteExpirationRelativeToNow);

        _mockCache
            .Setup(x => x.TryGetValue("WikiPageAccessRules", out cacheEntry))
            .Returns(false);

        _mockCache
            .Setup(x => x.CreateEntry("WikiPageAccessRules"))
            .Returns(cacheEntryMock.Object);

        _mockRepository
            .Setup(x => x.ReadFileAsync(".wikipermissions", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);
    }

    private IWikiUser CreateMockUser(string email, string name)
    {
        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.GitEmail).Returns(email);
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.DisplayName).Returns(name);
        return mockUser.Object;
    }

    #endregion
}
