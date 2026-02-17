using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class WikiPagePermissionHelperTest
{
    private readonly Mock<IWikiPageService> _mockPageService;
    private readonly Mock<IPageAccessControlService> _mockAccessControlService;
    private readonly WikiOptions _options;
    private readonly WikiPagePermissionHelper _helper;

    public WikiPagePermissionHelperTest()
    {
        _mockPageService = new Mock<IWikiPageService>();
        _mockAccessControlService = new Mock<IPageAccessControlService>();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            UsePageLevelPermissions = false,
            AllowAnonymousViewing = true
        };

        var optionsWrapper = Options.Create(_options);

        _helper = new WikiPagePermissionHelper(
            _mockPageService.Object,
            _mockAccessControlService.Object,
            optionsWrapper);
    }

    #region CanEdit Tests - Basic Scenarios

    [Fact]
    public async Task CanEdit_WithNullUser_ReturnsFalse()
    {
        // Act
        var result = await _helper.CanEdit(null, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEdit_WithUserWithoutEditPermission_ReturnsFalse()
    {
        // Arrange
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEdit_WithUserWithEditPermissionAndNoPageLevelPermissions_ReturnsTrue()
    {
        // Arrange
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEdit_WithNestedPageName_WorksCorrectly()
    {
        // Arrange
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });

        // Act
        var result = await _helper.CanEdit(mockUser, "Docs/Guide/Installation", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region CanEdit Tests - With Page-Level Permissions

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndUserCanEdit_ReturnsTrue()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "editors" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndUserCannotEdit_ReturnsFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "viewers" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsDisabled_DoesNotCheckPageAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndNullUser_ReturnsFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        // Act
        var result = await _helper.CanEdit(null, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndUserWithoutEditPermission_ReturnsFalseWithoutCheckingPageAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndAdminPage_ChecksCorrectly()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "admin" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true,
            MatchedPattern = "admin/**"
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("admin/settings", new[] { "admin" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanEdit(mockUser, "admin/settings", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEdit_WithPageLevelPermissionsAndMultipleGroups_PassesAllGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors", "admin", "users" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "editors", "admin", "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "editors", "admin", "users" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CanView Tests - Basic Scenarios

    [Fact]
    public async Task CanView_WithAnonymousViewingEnabledAndNullUser_ReturnsTrue()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        // Act
        var result = await _helper.CanView(null, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingDisabledAndNullUser_ReturnsFalse()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _helper.CanView(null, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingDisabledAndUserWithoutViewPermission_ReturnsFalse()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: false, canEdit: false, groups: new[] { "restricted" });

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingDisabledAndUserWithViewPermission_ReturnsTrue()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingEnabledAndUserWithViewPermission_ReturnsTrue()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanView_WithNestedPageName_WorksCorrectly()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanView(mockUser, "Docs/Guide/Installation", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region CanView Tests - With Page-Level Permissions

    [Fact]
    public async Task CanView_WithPageLevelPermissionsAndUserCanView_ReturnsTrue()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "viewers" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CanView_WithPageLevelPermissionsAndUserCannotView_ReturnsFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "restricted" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "restricted" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanView_WithPageLevelPermissionsAndAnonymousUser_ChecksWithEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanView(null, "PublicPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CanView_WithPageLevelPermissionsDisabled_DoesNotCheckPageAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CanView_WithPageLevelPermissionsAndAnonymousUserDenied_ReturnsFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false,
            MatchedPattern = "admin/**"
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("admin/settings", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanView(null, "admin/settings", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanView_WithPageLevelPermissionsAndUserWithMultipleGroups_PassesAllGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers", "editors", "admin" });

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "viewers", "editors", "admin" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "viewers", "editors", "admin" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingDisabledAndNullUser_ReturnsFalseWithoutCheckingPageAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _helper.CanView(null, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CanView_WithAnonymousViewingDisabledAndUserWithoutViewPermission_ReturnsFalseWithoutCheckingPageAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;
        var mockUser = CreateMockUser(canView: false, canEdit: false, groups: new[] { "restricted" });

        // Act
        var result = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetAllAccessiblePages Tests - Basic Scenarios

    [Fact]
    public async Task GetAllAccessiblePages_WithNoPageLevelPermissions_ReturnsAllPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page1", Title = "Page 1", Culture = null },
            new WikiPageInfo { PageName = "Page2", Title = "Page 2", Culture = null },
            new WikiPageInfo { PageName = "Page3", Title = "Page 3", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(allPages, result);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithEmptyPageList_ReturnsEmptyList()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiPageInfo>());

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithNullUser_ReturnsAllPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page1", Title = "Page 1", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        // Act
        var result = await _helper.GetAllAccessiblePages(null, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region GetAllAccessiblePages Tests - With Page-Level Permissions

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissions_FiltersBasedOnAccess()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Public", Title = "Public Page", Culture = null },
            new WikiPageInfo { PageName = "Admin", Title = "Admin Page", Culture = null },
            new WikiPageInfo { PageName = "Docs", Title = "Documentation", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Public", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Admin", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Docs", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PageName == "Public");
        Assert.Contains(result, p => p.PageName == "Docs");
        Assert.DoesNotContain(result, p => p.PageName == "Admin");
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissionsAndNullUser_FiltersWithEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Public", Title = "Public Page", Culture = null },
            new WikiPageInfo { PageName = "Admin", Title = "Admin Page", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Public", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Admin", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.PageName == "Public");
        Assert.DoesNotContain(result, p => p.PageName == "Admin");
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissionsAndNoAccessiblePages_ReturnsEmptyList()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Admin1", Title = "Admin Page 1", Culture = null },
            new WikiPageInfo { PageName = "Admin2", Title = "Admin Page 2", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissionsAndAllAccessiblePages_ReturnsAllPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page1", Title = "Page 1", Culture = null },
            new WikiPageInfo { PageName = "Page2", Title = "Page 2", Culture = null },
            new WikiPageInfo { PageName = "Page3", Title = "Page 3", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "admin" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissions_ChecksEachPageIndividually()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page1", Title = "Page 1", Culture = null },
            new WikiPageInfo { PageName = "Page2", Title = "Page 2", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Page1", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Page2", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("Page1", new[] { "viewers" }, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("Page2", new[] { "viewers" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAccessiblePages_WithPageLevelPermissionsAndNestedPages_FiltersCorrectly()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Public/Guide", Title = "Public Guide", Culture = null },
            new WikiPageInfo { PageName = "Docs/Private/Internal", Title = "Internal Docs", Culture = null },
            new WikiPageInfo { PageName = "Admin/Settings", Title = "Settings", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Docs/Public/Guide", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Docs/Private/Internal", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Admin/Settings", new[] { "viewers" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.PageName == "Docs/Public/Guide");
    }

    #endregion

    #region GetAllAccessiblePages Tests - Order Preservation

    [Fact]
    public async Task GetAllAccessiblePages_PreservesOriginalOrder()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null },
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null },
            new WikiPageInfo { PageName = "Mango", Title = "Mango", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // Act
        var result = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Zebra", result[0].PageName);
        Assert.Equal("Apple", result[1].PageName);
        Assert.Equal("Mango", result[2].PageName);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task CanEdit_PassesCancellationTokenToAccessControlService()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });
        var cancellationToken = new CancellationToken();

        var pageAccess = new PageAccessPermissions { CanRead = true, CanEdit = true };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "editors" }, cancellationToken))
            .ReturnsAsync(pageAccess);

        // Act
        await _helper.CanEdit(mockUser, "TestPage", cancellationToken);

        // Assert
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "editors" }, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CanView_PassesCancellationTokenToAccessControlService()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });
        var cancellationToken = new CancellationToken();

        var pageAccess = new PageAccessPermissions { CanRead = true, CanEdit = false };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "viewers" }, cancellationToken))
            .ReturnsAsync(pageAccess);

        // Act
        await _helper.CanView(mockUser, "TestPage", cancellationToken);

        // Assert
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "viewers" }, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAccessiblePages_PassesCancellationTokenToServices()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        var cancellationToken = new CancellationToken();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page1", Title = "Page 1", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(cancellationToken))
            .ReturnsAsync(allPages);

        var mockUser = CreateMockUser(canView: true, canEdit: false, groups: new[] { "viewers" });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Page1", new[] { "viewers" }, cancellationToken))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // Act
        await _helper.GetAllAccessiblePages(mockUser, cancellationToken);

        // Assert
        _mockPageService.Verify(
            x => x.GetAllPagesAsync(cancellationToken),
            Times.Once);
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("Page1", new[] { "viewers" }, cancellationToken),
            Times.Once);
    }

    #endregion

    #region Integration-Style Tests

    [Fact]
    public async Task CompleteWorkflow_AnonymousUserWithPageLevelPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = true;

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Public", Title = "Public Page", Culture = null },
            new WikiPageInfo { PageName = "Admin", Title = "Admin Page", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Public", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Admin", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        // Act & Assert - CanView
        var canViewPublic = await _helper.CanView(null, "Public", CancellationToken.None);
        var canViewAdmin = await _helper.CanView(null, "Admin", CancellationToken.None);
        Assert.True(canViewPublic);
        Assert.False(canViewAdmin);

        // Act & Assert - CanEdit
        var canEditPublic = await _helper.CanEdit(null, "Public", CancellationToken.None);
        var canEditAdmin = await _helper.CanEdit(null, "Admin", CancellationToken.None);
        Assert.False(canEditPublic); // Null user cannot edit
        Assert.False(canEditAdmin);

        // Act & Assert - GetAllAccessiblePages
        var accessiblePages = await _helper.GetAllAccessiblePages(null, CancellationToken.None);
        Assert.Single(accessiblePages);
        Assert.Contains(accessiblePages, p => p.PageName == "Public");
    }

    [Fact]
    public async Task CompleteWorkflow_AuthenticatedUserWithMixedPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        _options.AllowAnonymousViewing = false;

        var mockUser = CreateMockUser(canView: true, canEdit: true, groups: new[] { "editors" });

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs", Title = "Documentation", Culture = null },
            new WikiPageInfo { PageName = "Admin", Title = "Admin Page", Culture = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Docs", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("Admin", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // Act & Assert - CanView
        var canViewDocs = await _helper.CanView(mockUser, "Docs", CancellationToken.None);
        var canViewAdmin = await _helper.CanView(mockUser, "Admin", CancellationToken.None);
        Assert.True(canViewDocs);
        Assert.True(canViewAdmin);

        // Act & Assert - CanEdit
        var canEditDocs = await _helper.CanEdit(mockUser, "Docs", CancellationToken.None);
        var canEditAdmin = await _helper.CanEdit(mockUser, "Admin", CancellationToken.None);
        Assert.True(canEditDocs);
        Assert.False(canEditAdmin);

        // Act & Assert - GetAllAccessiblePages
        var accessiblePages = await _helper.GetAllAccessiblePages(mockUser, CancellationToken.None);
        Assert.Equal(2, accessiblePages.Count);
    }

    [Fact]
    public async Task CompleteWorkflow_UserWithNoPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;
        _options.AllowAnonymousViewing = false;

        var mockUser = CreateMockUser(canView: false, canEdit: false, groups: new[] { "restricted" });

        // Act & Assert - CanView
        var canView = await _helper.CanView(mockUser, "TestPage", CancellationToken.None);
        Assert.False(canView);

        // Act & Assert - CanEdit
        var canEdit = await _helper.CanEdit(mockUser, "TestPage", CancellationToken.None);
        Assert.False(canEdit);
    }

    #endregion

    #region Helper Methods

    private static IWikiUserWithPermissions CreateMockUser(bool canView, bool canEdit, string[] groups)
    {
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(canView);
        mockUser.Setup(x => x.CanEdit).Returns(canEdit);
        mockUser.Setup(x => x.Groups).Returns(groups);
        return mockUser.Object;
    }

    #endregion
}
