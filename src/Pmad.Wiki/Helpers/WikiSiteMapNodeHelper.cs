using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Helpers;

internal static class WikiSiteMapNodeHelper
{
    internal static List<WikiSiteMapNode> Build(List<WikiPageInfo> allPages, string neutralMarkdownPageCulture)
    {
        // Group pages by neutral culture (page name)
        var pageGroups = allPages.GroupBy(p => p.PageName).ToList();

        // Build hierarchy
        var rootNodes = new List<WikiSiteMapNode>();
        var nodesByPath = new Dictionary<string, WikiSiteMapNode>();

        foreach (var group in pageGroups.OrderBy(g => g.Key))
        {
            var pageName = group.Key;
            var parts = pageName.Split('/');

            WikiSiteMapNode? parentNode = null;
            var currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) currentPath += "/";
                currentPath += parts[i];

                if (!nodesByPath.TryGetValue(currentPath, out var node))
                {
                    if (currentPath == pageName)
                    {
                        var pageInfo = group.FirstOrDefault(p => p.Culture == null || p.Culture == neutralMarkdownPageCulture);
                        node = new WikiSiteMapNode
                        {
                            PageName = currentPath,
                            DisplayName = pageInfo?.Title ?? parts[i],
                            Title = pageInfo?.Title,
                            HasPage = true,
                            Culture = pageInfo?.Culture,
                            LastModified = pageInfo?.LastModified,
                            LastModifiedBy = pageInfo?.LastModifiedBy,
                            Level = i
                        };
                    }
                    else
                    {
                        node = new WikiSiteMapNode
                        {
                            PageName = currentPath,
                            DisplayName = parts[i],
                            HasPage = false,
                            Level = i
                        };
                    }

                    nodesByPath[currentPath] = node;

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

        return rootNodes;
    }
}
