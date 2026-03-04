using Pmad.Wiki.Models;

namespace Pmad.Wiki.Test.Models;

public class WikiGroupViewModelTest
{
    #region Constructor / Property Tests

    [Fact]
    public void Constructor_WithNameOnly_SetsName()
    {
        var vm = new WikiGroupViewModel("admins");

        Assert.Equal("admins", vm.Name);
        Assert.Null(vm.Label);
        Assert.Null(vm.Description);
        Assert.Equal("admins", vm.ActualLabel);
    }

    [Fact]
    public void Constructor_WithNameAndLabel_SetsNameAndLabel()
    {
        var vm = new WikiGroupViewModel("admins", "Administrators");

        Assert.Equal("admins", vm.Name);
        Assert.Equal("Administrators", vm.Label);
        Assert.Null(vm.Description);
        Assert.Equal("Administrators", vm.ActualLabel);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        var vm = new WikiGroupViewModel("admins", "Administrators", "Users with admin rights");

        Assert.Equal("admins", vm.Name);
        Assert.Equal("Administrators", vm.Label);
        Assert.Equal("Users with admin rights", vm.Description);
        Assert.Equal("Administrators", vm.ActualLabel);
    }

    [Fact]
    public void Constructor_WithNullLabelAndDescription_SetsNulls()
    {
        var vm = new WikiGroupViewModel("editors", null, null);

        Assert.Equal("editors", vm.Name);
        Assert.Null(vm.Label);
        Assert.Null(vm.Description);
        Assert.Equal("editors", vm.ActualLabel);
    }

    #endregion

    #region Tooltip Tests

    [Fact]
    public void Tooltip_WithNameOnly_ReturnsName()
    {
        var vm = new WikiGroupViewModel("admins");

        Assert.Equal("admins", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithNameAndLabel_ReturnsName()
    {
        var vm = new WikiGroupViewModel("admins", "Administrators");

        Assert.Equal("admins", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithNameAndDescription_ReturnsNameColonDescription()
    {
        var vm = new WikiGroupViewModel("admins", null, "Users with admin rights");

        Assert.Equal("admins: Users with admin rights", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithNameLabelAndDescription_ReturnsNameColonDescription()
    {
        var vm = new WikiGroupViewModel("admins", "Administrators", "Users with admin rights");

        Assert.Equal("admins: Users with admin rights", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithNameAndEmptyDescription_ReturnsName()
    {
        var vm = new WikiGroupViewModel("admins", null, "");

        Assert.Equal("admins", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithEmptyNameAndDescription_ReturnsDescription()
    {
        var vm = new WikiGroupViewModel("", null, "Some description");

        Assert.Equal("Some description", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithNullNameAndDescription_ReturnsDescription()
    {
        var vm = new WikiGroupViewModel(null!, null, "Some description");

        Assert.Equal("Some description", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithEmptyNameAndNullDescription_ReturnsNull()
    {
        var vm = new WikiGroupViewModel("", null, null);

        Assert.Null(vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithEmptyNameAndEmptyDescription_ReturnsEmptyString()
    {
        var vm = new WikiGroupViewModel("", null, "");

        Assert.Equal("", vm.Tooltip);
    }

    [Fact]
    public void Tooltip_WithAllNulls_ReturnsNull()
    {
        var vm = new WikiGroupViewModel(null!, null, null);

        Assert.Null(vm.Tooltip);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_TwoInstancesWithSameValues_AreEqual()
    {
        var vm1 = new WikiGroupViewModel("admins", "Administrators", "Admin users");
        var vm2 = new WikiGroupViewModel("admins", "Administrators", "Admin users");

        Assert.Equal(vm1, vm2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentName_AreNotEqual()
    {
        var vm1 = new WikiGroupViewModel("admins", "Administrators");
        var vm2 = new WikiGroupViewModel("editors", "Administrators");

        Assert.NotEqual(vm1, vm2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentLabel_AreNotEqual()
    {
        var vm1 = new WikiGroupViewModel("admins", "Administrators");
        var vm2 = new WikiGroupViewModel("admins", "Admins");

        Assert.NotEqual(vm1, vm2);
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentDescription_AreNotEqual()
    {
        var vm1 = new WikiGroupViewModel("admins", "Administrators", "Admin users");
        var vm2 = new WikiGroupViewModel("admins", "Administrators", "Different description");

        Assert.NotEqual(vm1, vm2);
    }

    #endregion
}
