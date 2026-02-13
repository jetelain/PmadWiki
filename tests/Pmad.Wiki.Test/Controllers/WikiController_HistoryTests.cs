using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_HistoryTests : WikiControllerTestBase
{
    #region History Action Tests

    [Fact]
    public async Task History_WithValidPageName_ReturnsViewWithHistory()
    {
        // Arrange
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Initial commit",
                AuthorName = "John Doe",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Update page",
                AuthorName = "Jane Smith",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Null(model.Culture);
        Assert.Equal(2, model.Entries.Count);
        Assert.Equal("commit1", model.Entries[0].CommitId);
        Assert.Equal("Initial commit", model.Entries[0].Message);
        Assert.Equal("John Doe", model.Entries[0].AuthorName);
        Assert.Equal("commit2", model.Entries[1].CommitId);
    }

    [Fact]
    public async Task History_WithCulture_ReturnsHistoryForSpecificCulture()
    {
        // Arrange
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Créer la page",
                AuthorName = "Jean Dupont",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Single(model.Entries);
        Assert.Equal("Créer la page", model.Entries[0].Message);
    }

    [Fact]
    public async Task History_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("NewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiHistoryItem>());

        // Act
        var result = await _controller.History("NewPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Equal("NewPage", model.PageName);
        Assert.Empty(model.Entries);
    }

    [Fact]
    public async Task History_WithEmptyPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.History("", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task History_WithNullPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.History(null!, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task History_WithInvalidPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.History("../../../etc/passwd", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid page name.", badRequestResult.Value);
    }

    [Fact]
    public async Task History_WithInvalidCulture_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.History("TestPage", "invalid-culture-code", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid culture identifier.", badRequestResult.Value);
    }

    [Fact]
    public async Task History_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task History_WhenUserAuthenticatedButCannotView_ReturnsForbid()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task History_WithAuthenticatedUserAndCanView_ReturnsHistory()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Single(model.Entries);
    }

    [Fact]
    public async Task History_WithPageLevelPermissionsEnabledAndNoReadAccess_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("AdminPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task History_WithPageLevelPermissionsEnabledAndUnauthenticatedUser_ReturnsChallenge()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _controller.History("AdminPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task History_WithPageLevelPermissionsAndReadAccess_ReturnsHistory()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Single(model.Entries);
    }

    [Fact]
    public async Task History_WithNestedPagePath_ReturnsHistory()
    {
        // Arrange
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Initial version",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/api/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("docs/api/reference", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("docs/api/reference", model.PageName);
        Assert.Single(model.Entries);
    }

    [Fact]
    public async Task History_WithMultipleCommits_PreservesOrder()
    {
        // Arrange
        var timestamp1 = DateTimeOffset.UtcNow.AddDays(-5);
        var timestamp2 = DateTimeOffset.UtcNow.AddDays(-3);
        var timestamp3 = DateTimeOffset.UtcNow.AddDays(-1);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First commit",
                AuthorName = "Author1",
                Timestamp = timestamp1
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second commit",
                AuthorName = "Author2",
                Timestamp = timestamp2
            },
            new WikiHistoryItem
            {
                CommitId = "commit3",
                Message = "Third commit",
                AuthorName = "Author3",
                Timestamp = timestamp3
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.Entries.Count);
        Assert.Equal("commit1", model.Entries[0].CommitId);
        Assert.Equal("commit2", model.Entries[1].CommitId);
        Assert.Equal("commit3", model.Entries[2].CommitId);
        Assert.Equal(timestamp1, model.Entries[0].Timestamp);
        Assert.Equal(timestamp2, model.Entries[1].Timestamp);
        Assert.Equal(timestamp3, model.Entries[2].Timestamp);
    }

    [Fact]
    public async Task History_WithAnonymousUserAndAllowAnonymousViewing_ReturnsHistory()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Public commit",
                AuthorName = "Public Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("PublicPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);
        Assert.Single(model.Entries);
    }

    [Fact]
    public async Task History_WithPageLevelPermissionsAndAnonymousUser_UsesEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("PublicPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);

        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task History_WithAuthenticatedUserAndNullWikiUser_AllowsIfAnonymousViewingEnabled()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
    }

    [Fact]
    public async Task History_WithLongCommitIds_ReturnsFullCommitIds()
    {
        // Arrange
        var longCommitId = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0";
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = longCommitId,
                Message = "Test commit",
                AuthorName = "Test Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Single(model.Entries);
        Assert.Equal(longCommitId, model.Entries[0].CommitId);
    }

    [Fact]
    public async Task History_WithSpecialCharactersInMessage_PreservesMessage()
    {
        // Arrange
        var specialMessage = "Fix: Added <strong>bold</strong> & 'quoted' text with \"quotes\"";
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = specialMessage,
                AuthorName = "Test Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Single(model.Entries);
        Assert.Equal(specialMessage, model.Entries[0].Message);
    }

    [Fact]
    public async Task History_WithDifferentAuthors_PreservesAuthorNames()
    {
        // Arrange
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Alice Johnson",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Bob Smith",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit3",
                Message = "Third",
                AuthorName = "Alice Johnson",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.Entries.Count);
        Assert.Equal("Alice Johnson", model.Entries[0].AuthorName);
        Assert.Equal("Bob Smith", model.Entries[1].AuthorName);
        Assert.Equal("Alice Johnson", model.Entries[2].AuthorName);
    }

    [Fact]
    public async Task History_WithCultureAndPageLevelPermissions_ChecksPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "translators" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Traduction initiale",
                AuthorName = "Translator",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.History("TestPage", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Single(model.Entries);

        // Note: Permission check is on the page name, not the culture-specific version
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task History_MapsAllHistoryItemPropertiesToEntries()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "abc123def456",
                Message = "Complete message text",
                AuthorName = "Full Author Name",
                Timestamp = timestamp
            }
        };

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.History("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        
        Assert.Single(model.Entries);
        var entry = model.Entries[0];
        Assert.Equal("abc123def456", entry.CommitId);
        Assert.Equal("Complete message text", entry.Message);
        Assert.Equal("Full Author Name", entry.AuthorName);
        Assert.Equal(timestamp, entry.Timestamp);
    }

    #endregion
}
