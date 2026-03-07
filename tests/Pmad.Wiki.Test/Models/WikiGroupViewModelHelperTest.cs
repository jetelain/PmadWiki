using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Models;

public class WikiGroupViewModelHelperTest
{
    [Fact]
    public void ResolveGroup_WhenGroupExists_ReturnsViewModelWithAllGroupProperties()
    {
        var mockGroup = new Mock<IWikiUserGroup>();
        mockGroup.Setup(g => g.Label).Returns("Administrators");
        mockGroup.Setup(g => g.Description).Returns("Users with admin rights");
        mockGroup.Setup(g => g.Color).Returns(WikiColor.Primary);

        var groups = new Dictionary<string, IWikiUserGroup>
        {
            { "admins", mockGroup.Object }
        };

        var result = groups.ResolveGroup("admins");

        Assert.Equal("admins", result.Name);
        Assert.Equal("Administrators", result.Label);
        Assert.Equal("Users with admin rights", result.Description);
        Assert.Equal(WikiColor.Primary, result.Color);
    }

    [Fact]
    public void ResolveGroup_WhenGroupNotFound_ReturnsViewModelWithNameOnly()
    {
        var groups = new Dictionary<string, IWikiUserGroup>();

        var result = groups.ResolveGroup("unknown");

        Assert.Equal("unknown", result.Name);
        Assert.Null(result.Label);
        Assert.Null(result.Description);
        Assert.Equal(WikiColor.Secondary, result.Color);
    }

    [Fact]
    public void ResolveGroup_WhenGroupExistsWithNullLabelAndDescription_ReturnsViewModelWithNulls()
    {
        var mockGroup = new Mock<IWikiUserGroup>();
        mockGroup.Setup(g => g.Label).Returns((string?)null);
        mockGroup.Setup(g => g.Description).Returns((string?)null);
        mockGroup.Setup(g => g.Color).Returns(WikiColor.Secondary);

        var groups = new Dictionary<string, IWikiUserGroup>
        {
            { "editors", mockGroup.Object }
        };

        var result = groups.ResolveGroup("editors");

        Assert.Equal("editors", result.Name);
        Assert.Null(result.Label);
        Assert.Null(result.Description);
        Assert.Equal(WikiColor.Secondary, result.Color);
    }
}
