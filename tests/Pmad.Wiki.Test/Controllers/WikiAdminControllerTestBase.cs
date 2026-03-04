using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Resources;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public abstract class WikiAdminControllerTestBase
{
    protected readonly Mock<IWikiUserService> _mockUserService;
    protected readonly Mock<IPageAccessControlService> _mockAccessControlService;
    protected readonly Mock<ILogger<WikiAdminController>> _mockLogger;
    protected readonly Mock<IStringLocalizer<WikiResources>> _mockLocalizer;
    protected readonly WikiOptions _options;
    protected readonly WikiAdminController _controller;

    public WikiAdminControllerTestBase()
    {
        _mockUserService = new Mock<IWikiUserService>();
        _mockAccessControlService = new Mock<IPageAccessControlService>();
        _mockLogger = new Mock<ILogger<WikiAdminController>>();
        _mockLocalizer = new Mock<IStringLocalizer<WikiResources>>();

        // Setup default localizer behavior to return the key as the value
        _mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _mockLocalizer
            .Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            NeutralMarkdownPageCulture = "en",
            HomePageName = "Home",
            AllowAnonymousViewing = true,
            UsePageLevelPermissions = false,
            AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".mp4" }
        };

        var optionsWrapper = Options.Create(_options);

        _controller = new WikiAdminController(
            _mockUserService.Object,
            _mockAccessControlService.Object,
            optionsWrapper,
            _mockLogger.Object,
            _mockLocalizer.Object);

        // Setup default HTTP context
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        _controller.ControllerContext = new ControllerContext(actionContext);

        // Mock URL helper to return test URLs
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns((UrlActionContext context) =>
            {
                var id = (context.Values as RouteValueDictionary)?["id"]?.ToString() ?? "unknown";
                return $"/Wiki/{context.Action}/{id}";
            });
        _controller.Url = mockUrlHelper.Object;
    }

    protected void SetupUserContext(string userName)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "TestAuth"));
        var httpContext = new DefaultHttpContext { User = user };

        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        _controller.ControllerContext = new ControllerContext(actionContext);
    }

    protected static Mock<IWikiUserGroup> CreateGroup(string name, string? label, string? description = null)
    {
        var mock = new Mock<IWikiUserGroup>();
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.Label).Returns(label);
        mock.Setup(x => x.Description).Returns(description);
        return mock;
    }
}
