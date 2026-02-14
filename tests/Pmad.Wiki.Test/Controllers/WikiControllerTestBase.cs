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
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Controllers;

public abstract class WikiControllerTestBase
{
    protected readonly Mock<IWikiPageService> _mockPageService;
    protected readonly Mock<IWikiUserService> _mockUserService;
    protected readonly Mock<IPageAccessControlService> _mockAccessControlService;
    protected readonly Mock<IMarkdownRenderService> _mockMarkdownRenderService;
    protected readonly Mock<ITemporaryMediaStorageService> _mockTemporaryMediaStorage;
    protected readonly Mock<IWikiPageEditService> _mockWikiPageEditService;
    protected readonly Mock<ILogger<WikiController>> _mockLogger;
    protected readonly Mock<IStringLocalizer<WikiResources>> _mockLocalizer;
    protected readonly WikiOptions _options;
    protected readonly WikiController _controller;
    protected readonly LinkGenerator _linkGenerator;

    public WikiControllerTestBase()
    {
        _mockPageService = new Mock<IWikiPageService>();
        _mockUserService = new Mock<IWikiUserService>();
        _mockAccessControlService = new Mock<IPageAccessControlService>();
        _mockMarkdownRenderService = new Mock<IMarkdownRenderService>();
        _mockTemporaryMediaStorage = new Mock<ITemporaryMediaStorageService>();
        _mockWikiPageEditService = new Mock<IWikiPageEditService>();
        _mockLogger = new Mock<ILogger<WikiController>>();
        _mockLocalizer = new Mock<IStringLocalizer<WikiResources>>();
        _linkGenerator = new TestLinkGenerator();

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

        _controller = new WikiController(
            _mockPageService.Object,
            _mockUserService.Object,
            _mockAccessControlService.Object,
            _mockMarkdownRenderService.Object,
            _mockTemporaryMediaStorage.Object,
            _mockWikiPageEditService.Object,
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

    protected static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    protected void SetupUserContext(string userName)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "TestAuth"));
        var httpContext = new DefaultHttpContext { User = user };

        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        _controller.ControllerContext = new ControllerContext(actionContext);
    }
}
