using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Helpers;

internal static class WikiSiteMapNodeHelper
{
    internal static List<WikiSiteMapNode> Build(List<WikiPageInfo> allPages, string culture)
    {
        return BuildInternal(allPages, culture, parentPrefix: null);
    }

    internal static List<WikiSiteMapNode> BuildSubPages(List<WikiPageInfo> subPages, string culture, string parentPageName)
    {
        return BuildInternal(subPages, culture, parentPrefix: parentPageName + "/");
    }

    private static List<WikiSiteMapNode> BuildInternal(List<WikiPageInfo> allPages, string culture, string? parentPrefix)
    {
        // Group pages by neutral culture (page name)
        var pageGroups = allPages.GroupBy(p => p.PageName).ToList();

        // Build hierarchy
        var rootNodes = new List<WikiSiteMapNode>();
        var nodesByPath = new Dictionary<string, WikiSiteMapNode>();

        foreach (var group in pageGroups.OrderBy(g => g.Key))
        {
            var pageName = group.Key;

            // Strip the parent prefix so paths and levels are relative to the parent page
            var relativeName = parentPrefix != null && pageName.StartsWith(parentPrefix, StringComparison.Ordinal)
                ? pageName[parentPrefix.Length..]
                : pageName;

            var parts = relativeName.Split('/');

            WikiSiteMapNode? parentNode = null;
            var currentRelativePath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) currentRelativePath += "/";
                currentRelativePath += parts[i];

                var currentFullPath = parentPrefix != null ? parentPrefix + currentRelativePath : currentRelativePath;

                if (!nodesByPath.TryGetValue(currentFullPath, out var node))
                {
                    if (currentFullPath == pageName)
                    {
                        var pageInfo = group.FirstOrDefault(p => p.Culture == culture) 
                            ?? group.FirstOrDefault(p => p.Culture == null)
                            ?? group.OrderBy(p => p.Culture).First();
                        node = new WikiSiteMapNode
                        {
                            PageName = currentFullPath,
                            DisplayName = pageInfo?.Title ?? parts[i],
                            Title = pageInfo?.Title,
                            HasPage = true,
                            Culture = pageInfo?.Culture,
                            LastModified = pageInfo?.LastModified,
                            LastModifiedBy = pageInfo?.LastModifiedBy,
                            Level = i,
                            SortOrder = pageInfo?.SortOrder ?? 0
                        };
                    }
                    else
                    {
                        node = new WikiSiteMapNode
                        {
                            PageName = currentFullPath,
                            DisplayName = parts[i],
                            HasPage = false,
                            Level = i
                        };
                    }

                    nodesByPath[currentFullPath] = node;

                    if (parentNode != null)
                    {
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        rootNodes.Add(node);
                    }
                }

                parentNode = node;
            }
        }

        SortNodes(rootNodes);
        return rootNodes;
    }

    private static void SortNodes(List<WikiSiteMapNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            var cmp = a.SortOrder.CompareTo(b.SortOrder);
            return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        });
        foreach (var node in nodes)
        {
            SortNodes(node.Children);
        }
    }
}
